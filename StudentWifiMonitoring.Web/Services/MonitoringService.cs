using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Domain;

namespace StudentWifiMonitoring.Web.Services
{
    public class MonitoringService : BackgroundService
    {
        private readonly ILogger<MonitoringService> _logger;
        private readonly IServiceProvider _sp;
        private readonly IStationProvider _provider;
        private readonly TimeSpan _interval;

        public MonitoringService(ILogger<MonitoringService> logger, IServiceProvider sp, IStationProvider provider, IConfiguration cfg)
        {
            _logger = logger;
            _sp = sp;
            _provider = provider;
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

                    // Use explicit time range expression so EF Core can translate it.
                    var activeTest = await db.TestSessions
                        .OrderByDescending(t => t.Id)
                        .FirstOrDefaultAsync(t => DateTime.UtcNow >= t.StartTime && DateTime.UtcNow <= t.EndTime, stoppingToken);

                    if (activeTest == null)
                    {
                        // No active test: nothing to log
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
                            _logger.LogInformation("Student {Mac} connected (student: {StudentId})", mac, student.Id);
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
                            _logger.LogInformation("Student {Mac} disconnected (student: {StudentId})", mac, student.Id);
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
