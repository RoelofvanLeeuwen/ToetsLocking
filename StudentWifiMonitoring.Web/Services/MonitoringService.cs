using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Domain;
using StudentWifiMonitoring.Web.Hubs;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Achtergrondservice die periodiek verbonden WiFi-stations monitort.
/// Detecteert nieuwe verbindingen en verbroken verbindingen en registreert deze in de database.
/// Stuurt real-time updates via SignalR naar verbonden clients.
/// </summary>
public class MonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IStationProvider _stationProvider;
    private readonly IHubContext<StatusHub> _hubContext;
    private readonly ILogger<MonitoringService> _logger;
    private readonly int _pollSeconds;
    private readonly HashSet<string> _onlineMacs = new();
    private bool _hadActiveSession = false;

    public MonitoringService(
        IServiceProvider serviceProvider,
        IStationProvider stationProvider,
        IHubContext<StatusHub> hubContext,
        IConfiguration configuration,
        ILogger<MonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _stationProvider = stationProvider;
        _hubContext = hubContext;
        _logger = logger;
        _pollSeconds = configuration.GetValue<int>("Monitoring:PollSeconds", 2);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MonitoringService gestart met poll interval van {PollSeconds} seconden", _pollSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessStationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout tijdens het verwerken van stations");
            }

            await Task.Delay(TimeSpan.FromSeconds(_pollSeconds), stoppingToken);
        }

        _logger.LogInformation("MonitoringService gestopt");
    }

    /// <summary>
    /// Haalt de huidige verbonden stations op en verwerkt nieuwe connecties en disconnecties.
    /// </summary>
    private async Task ProcessStationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // Controleer of er een actieve toets is
        var activeSession = await context.TestSessions
            .Where(ts => ts.StartTime <= now && ts.EndTime >= now)
            .OrderBy(ts => ts.StartTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSession == null)
        {
            // Bij overgang van actief naar geen toets: sluit alle open verbindingen.
            // Zonder dit blijven verbindingen eeuwig open staan als een toets afloopt
            // terwijl een student nog op het WiFi zit.
            if (_hadActiveSession)
            {
                var openConnections = await context.Connections
                    .Where(c => c.DisconnectedAt == null)
                    .ToListAsync(cancellationToken);
                if (openConnections.Count > 0)
                {
                    var closeTime = DateTime.UtcNow;
                    foreach (var conn in openConnections)
                        conn.DisconnectedAt = closeTime;
                    await context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("{Count} open verbinding(en) gesloten na aflopen toetssessie", openConnections.Count);
                }
                _hadActiveSession = false;
            }
            _onlineMacs.Clear();
            return;
        }
        _hadActiveSession = true;

        // Haal huidige verbonden stations op
        var stations = await _stationProvider.GetStationsAsync();
        var currentMacs = stations.Select(s => s.MacAddress).ToHashSet();

        // Detecteer nieuwe connecties
        var newConnections = currentMacs.Except(_onlineMacs).ToList();
        foreach (var mac in newConnections)
        {
            await HandleConnectAsync(context, activeSession, mac, cancellationToken);
        }

        // Detecteer disconnecties
        var disconnections = _onlineMacs.Except(currentMacs).ToList();
        foreach (var mac in disconnections)
        {
            await HandleDisconnectAsync(context, activeSession, mac, cancellationToken);
        }

        // Herstel studenten die al verbonden waren maar geen open verbinding hebben
        await HandleAlreadyConnectedStudentsAsync(context, activeSession, currentMacs, cancellationToken);

        // Update de online macs set
        _onlineMacs.Clear();
        foreach (var mac in currentMacs)
        {
            _onlineMacs.Add(mac);
        }
    }

    /// <summary>
    /// Verwerkt studenten die al verbonden waren met de WiFi vóór of tijdens hun registratie,
    /// maar daardoor nooit via HandleConnectAsync zijn opgepikt.
    /// Als een bekend MAC online is zonder open verbinding in de database, wordt alsnog een verbinding aangemaakt.
    /// </summary>
    private async Task HandleAlreadyConnectedStudentsAsync(AppDbContext context, TestSession activeSession, HashSet<string> currentMacs, CancellationToken cancellationToken)
    {
        if (currentMacs.Count == 0) return;

        // Zoek studenten waarvan het MAC nu online is
        var onlineStudents = await context.Students
            .Where(s => currentMacs.Contains(s.MacAddress))
            .ToListAsync(cancellationToken);

        foreach (var student in onlineStudents)
        {
            if (student.IsTestComplete) continue;

            // Sla over als de student al een open verbinding heeft
            bool hasOpenConnection = await context.Connections
                .AnyAsync(c => c.StudentId == student.Id && c.DisconnectedAt == null, cancellationToken);

            if (hasOpenConnection) continue;

            // Student is online maar heeft geen open verbinding - herstel dit
            _logger.LogInformation("Student {Name} ({MacAddress}) was al verbonden maar had geen open verbinding, alsnog registreren",
                student.Name, student.MacAddress);

            var now = DateTime.UtcNow;
            context.Connections.Add(new Connection
            {
                StudentId = student.Id,
                ConnectedAt = now
            });
            context.Events.Add(new EventLog
            {
                StudentId = student.Id,
                TestSessionId = activeSession.Id,
                EventType = EventType.Connected,
                Timestamp = now
            });

            await context.SaveChangesAsync(cancellationToken);

            await _hubContext.Clients.All.SendAsync("status", new
            {
                mac = student.MacAddress,
                status = "connected",
                name = student.Name,
                testName = activeSession.Name
            }, cancellationToken);
        }
    }

    /// <summary>
    /// Verwerkt een nieuwe verbinding van een student.
    /// </summary>
    private async Task HandleConnectAsync(AppDbContext context, TestSession activeSession, string mac, CancellationToken cancellationToken)
    {
        var student = await context.Students
            .FirstOrDefaultAsync(s => s.MacAddress == mac, cancellationToken);

        if (student == null)
        {
            _logger.LogWarning("Onbekend MAC-adres verbonden: {MacAddress}", mac);
            return;
        }

        var now = DateTime.UtcNow;

        if (student.IsTestComplete)
        {
            // Student heeft eerder 'Ik ben klaar' geklikt maar verbindt opnieuw:
            // reset de vlag zodat monitoring hervat en het dashboard de ✅ verwijdert.
            student.IsTestComplete = false;
            _logger.LogInformation("Student {Name} ({MacAddress}) verbindt opnieuw na 'Ik ben klaar', monitoring hervat", student.Name, mac);
        }

        // Sluit eventuele stale open verbindingen van vorige sessies voordat we een nieuwe aanmaken.
        // HandleConnectAsync wordt aangeroepen als een MAC opnieuw verschijnt na een periode offline —
        // een al-open verbinding is dan een overblijfsel van een verlopen toets.
        var staleConnections = await context.Connections
            .Where(c => c.StudentId == student.Id && c.DisconnectedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var stale in staleConnections)
            stale.DisconnectedAt = now;

        // Voeg nieuwe verbinding toe
        var connection = new Connection
        {
            StudentId = student.Id,
            ConnectedAt = now
        };
        context.Connections.Add(connection);

        // Voeg event log toe
        var eventLog = new EventLog
        {
            StudentId = student.Id,
            TestSessionId = activeSession.Id,
            EventType = EventType.Connected,
            Timestamp = now
        };
        context.Events.Add(eventLog);

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Student {Name} ({MacAddress}) verbonden voor toets {TestName}", 
            student.Name, mac, activeSession.Name);

        // Stuur SignalR event
        await _hubContext.Clients.All.SendAsync("status", new
        {
            mac,
            status = "connected",
            name = student.Name,
            testName = activeSession.Name
        }, cancellationToken);
    }

    /// <summary>
    /// Verwerkt een verbroken verbinding van een student.
    /// </summary>
    private async Task HandleDisconnectAsync(AppDbContext context, TestSession activeSession, string mac, CancellationToken cancellationToken)
    {
        var student = await context.Students
            .FirstOrDefaultAsync(s => s.MacAddress == mac, cancellationToken);

        if (student == null)
        {
            _logger.LogWarning("Onbekend MAC-adres verbroken: {MacAddress}", mac);
            return;
        }

        var now = DateTime.UtcNow;

        // Sluit alle open verbindingen voor deze student — er zou er normaal maar één zijn,
        // maar meerdere zijn mogelijk als een vorige sessie niet netjes is afgesloten.
        var openConnections = await context.Connections
            .Where(c => c.StudentId == student.Id && c.DisconnectedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var conn in openConnections)
            conn.DisconnectedAt = now;

        // Voeg event log toe
        var eventLog = new EventLog
        {
            StudentId = student.Id,
            TestSessionId = activeSession.Id,
            EventType = EventType.Disconnected,
            Timestamp = now
        };
        context.Events.Add(eventLog);

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Student {Name} ({MacAddress}) verbroken voor toets {TestName}", 
            student.Name, mac, activeSession.Name);

        // Stuur SignalR event
        await _hubContext.Clients.All.SendAsync("status", new
        {
            mac,
            status = "disconnected",
            name = student.Name,
            testName = activeSession.Name
        }, cancellationToken);
    }
}
