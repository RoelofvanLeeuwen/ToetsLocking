using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Domain;
using StudentWifiMonitoring.Web.DTOs.Dashboard;
using StudentWifiMonitoring.Web.DTOs.Tests;
using StudentWifiMonitoring.Web.Services.Interfaces;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor het beheren van dashboard data zoals studenten, toetssessies en online status.
/// Bevat de businesslogica voor het filteren en ophalen van student- en connectiegegevens.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Haalt alle toetssessies op als DTO's, gesorteerd op starttijd (nieuwste eerst).
    /// </summary>
    public List<TestSessionDto> GetTestSessions()
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-14);
        return _db.TestSessions
            .Where(t => t.StartTime >= cutoff)
            .OrderByDescending(t => t.StartTime)
            .Select(t => new TestSessionDto
            {
                Id = t.Id,
                Name = t.Name,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                IsActive = t.StartTime <= now && t.EndTime >= now
            })
            .ToList();
    }

    /// <summary>
    /// Haalt studenten met hun online status op als DTO's, optioneel gefilterd op een specifieke toetssessie.
    /// </summary>
    /// <param name="testSessionId">Optioneel: ID van de toetssessie om op te filteren.</param>
    /// <returns>Lijst van student status DTO's inclusief online indicator en aantal verbroken verbindingen.</returns>
    public List<StudentStatusDto> GetStudentsWithStatus(int? testSessionId)
    {
        HashSet<string> onlineMacs;
        Dictionary<int, int> disconnectionCounts;
        Dictionary<int, DateTime> lastConnectionEvents;

        if (testSessionId.HasValue)
        {
            var test = _db.TestSessions.Find(testSessionId.Value);
            if (test == null)
            {
                return new List<StudentStatusDto>();
            }

            // Alleen verbindingen die open zijn én gestart zijn vanaf de starttijd van deze sessie
            // tellen als "online". Stale open verbindingen van verlopen toetsen (ConnectedAt vóór
            // test.StartTime) worden zo uitgesloten — zelfde principe als de groen-scherm fix.
            onlineMacs = _db.Connections
                .Where(c => c.DisconnectedAt == null
                            && c.Student!.TestName == test.Name
                            && c.ConnectedAt >= test.StartTime)
                .Select(c => c.Student!.MacAddress)
                .ToHashSet();

            disconnectionCounts = _db.Events
                .Where(e => e.EventType == EventType.Disconnected && e.TestSessionId == testSessionId.Value)
                .GroupBy(e => e.StudentId)
                .Select(g => new { StudentId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.StudentId, x => x.Count);

            // Gebruik EventLog (gefilterd op sessie) voor de meest recente "Verbonden" gebeurtenis
            lastConnectionEvents = _db.Events
                .Where(e => e.EventType == EventType.Connected && e.TestSessionId == testSessionId.Value)
                .GroupBy(e => e.StudentId)
                .Select(g => new { StudentId = g.Key, LastConnectedAt = g.Max(e => e.Timestamp) })
                .ToDictionary(x => x.StudentId, x => x.LastConnectedAt);

            return _db.Students
                .Where(s => s.TestName == test.Name)
                .Select(s => new StudentStatusDto
                {
                    MacAddress = s.MacAddress,
                    Name = s.Name,
                    TestName = s.TestName,
                    IsOnline = onlineMacs.Contains(s.MacAddress),
                    DisconnectionCount = disconnectionCounts.GetValueOrDefault(s.Id, 0),
                    LastConnectedAt = lastConnectionEvents.ContainsKey(s.Id) ? lastConnectionEvents[s.Id] : (DateTime?)null,
                    IsTestComplete = s.IsTestComplete
                })
                .ToList();
        }
        else
        {
            onlineMacs = _db.Connections
                .Where(c => c.DisconnectedAt == null)
                .Select(c => c.Student!.MacAddress)
                .ToHashSet();

            disconnectionCounts = _db.Events
                .Where(e => e.EventType == EventType.Disconnected)
                .GroupBy(e => e.StudentId)
                .Select(g => new { StudentId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.StudentId, x => x.Count);

            lastConnectionEvents = _db.Events
                .Where(e => e.EventType == EventType.Connected)
                .GroupBy(e => e.StudentId)
                .Select(g => new { StudentId = g.Key, LastConnectedAt = g.Max(e => e.Timestamp) })
                .ToDictionary(x => x.StudentId, x => x.LastConnectedAt);

            return _db.Students
                .Select(s => new StudentStatusDto
                {
                    MacAddress = s.MacAddress,
                    Name = s.Name,
                    TestName = s.TestName,
                    IsOnline = onlineMacs.Contains(s.MacAddress),
                    DisconnectionCount = disconnectionCounts.GetValueOrDefault(s.Id, 0),
                    LastConnectedAt = lastConnectionEvents.ContainsKey(s.Id) ? lastConnectionEvents[s.Id] : (DateTime?)null,
                    IsTestComplete = s.IsTestComplete
                })
                .ToList();
        }
    }

    /// <summary>
    /// Haalt de volledige activiteitenlog op van een student, optioneel gefilterd op een toetssessie.
    /// </summary>
    public List<StudentActivityDto> GetStudentActivity(string macAddress, int? testSessionId)
    {
        var query = _db.Events
            .Where(e => e.Student!.MacAddress == macAddress);

        if (testSessionId.HasValue)
            query = query.Where(e => e.TestSessionId == testSessionId.Value);

        return query
            .OrderBy(e => e.Timestamp)
            .Select(e => new StudentActivityDto
            {
                Timestamp = e.Timestamp,
                EventType = e.EventType
            })
            .ToList();
    }
}