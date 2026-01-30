using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web2.Data;
using StudentWifiMonitoring.Web2.Domain;
using StudentWifiMonitoring.Web2.Hubs;

namespace StudentWifiMonitoring.Web2.Services
{
    public class MonitoringService : BackgroundService
    {
        private readonly ILogger<MonitoringService> _logger;
        private readonly IServiceProvider _sp;
        private readonly IStationProvider _provider;
        private readonly IHubContext<StatusHub> _hub;
        private readonly TimeSpan _interval;

        public MonitoringService(ILogger<MonitoringService> logger, IServiceProvider sp, IStationProvider provider, IConfiguration cfg, IHubContext<StatusHub> hub)
        {
            _logger = logger; _sp = sp; _provider = provider; _hub = hub;
            _interval = TimeSpan.FromSeconds(int.TryParse(cfg["Monitoring:PollSeconds"], out var s) ? s : 2);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var knownOnline = new HashSet<string>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Check of er een actieve toets is
                    var activeTest = await db.TestSessions
                        .OrderByDescending(t => t.Id)
                        .FirstOrDefaultAsync(t => t.IsActive, stoppingToken);

                    if (activeTest == null)
                    {
                        // Geen actieve toets: niets loggen of broadcasten
                        await Task.Delay(_interval, stoppingToken);
                        continue;
                    }

                    var stations = await _provider.GetStationsAsync();
                    var current = stations.Select(s => s.MacAddress).ToHashSet();

                    var connectedNow = current.Except(knownOnline).ToList();
                    var disconnectedNow = knownOnline.Except(current).ToList();

                    // CONNECTS
                    foreach (var mac in connectedNow)
                    {
                        var student = await db.Students.FirstOrDefaultAsync(s => s.MacAddress == mac, stoppingToken);
                        if (student != null)
                        {
                            db.Connections.Add(new Connection { StudentId = student.Id, ConnectedAt = DateTime.UtcNow });
                            db.Events.Add(new EventLog { StudentId = student.Id, TestSessionId = activeTest.Id, EventType = EventType.Connected, Timestamp = DateTime.UtcNow });
                            await db.SaveChangesAsync(stoppingToken);
                            await _hub.Clients.All.SendAsync("status", new { mac, status = "connected", name = student.Name, testName = student.TestName }, stoppingToken);
                        }
                    }

                    // DISCONNECTS
                    foreach (var mac in disconnectedNow)
                    {
                        var student = await db.Students.FirstOrDefaultAsync(s => s.MacAddress == mac, stoppingToken);
                        if (student != null)
                        {
                            var openConn = await db.Connections
                                .Where(c => c.StudentId == student.Id && c.DisconnectedAt == null)
                                .OrderByDescending(c => c.ConnectedAt)
                                .FirstOrDefaultAsync(stoppingToken);
                            if (openConn != null) openConn.DisconnectedAt = DateTime.UtcNow;

                            db.Events.Add(new EventLog { StudentId = student.Id, TestSessionId = activeTest.Id, EventType = EventType.Disconnected, Timestamp = DateTime.UtcNow });
                            await db.SaveChangesAsync(stoppingToken);
                            await _hub.Clients.All.SendAsync("status", new { mac, status = "disconnected", name = student.Name, testName = student.TestName }, stoppingToken);
                        }
                    }

                    knownOnline = current;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Monitoring tick failed");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
