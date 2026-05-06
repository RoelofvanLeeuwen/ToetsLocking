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
            // Geen actieve toets, reset de online macs en stop
            _onlineMacs.Clear();
            return;
        }

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

        // Update de online macs set
        _onlineMacs.Clear();
        foreach (var mac in currentMacs)
        {
            _onlineMacs.Add(mac);
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

        // Zoek de laatste open verbinding
        var openConnection = await context.Connections
            .Where(c => c.StudentId == student.Id && c.DisconnectedAt == null)
            .OrderByDescending(c => c.ConnectedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (openConnection != null)
        {
            openConnection.DisconnectedAt = now;
        }

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
