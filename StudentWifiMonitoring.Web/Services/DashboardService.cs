using StudentWifiMonitoring.Web.Data;
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
        return _db.TestSessions
            .OrderByDescending(t => t.StartTime)
            .Select(t => new TestSessionDto
            {
                Id = t.Id,
                Name = t.Name,
                StartTime = t.StartTime,
                EndTime = t.EndTime
            })
            .ToList();
    }

    /// <summary>
    /// Haalt studenten met hun online status op als DTO's, optioneel gefilterd op een specifieke toetssessie.
    /// </summary>
    /// <param name="testSessionId">Optioneel: ID van de toetssessie om op te filteren.</param>
    /// <returns>Lijst van student status DTO's inclusief online indicator.</returns>
    public List<StudentStatusDto> GetStudentsWithStatus(int? testSessionId)
    {
        List<string> studentMacs;
        HashSet<string> onlineMacs;

        if (testSessionId.HasValue)
        {
            var test = _db.TestSessions.Find(testSessionId.Value);
            if (test == null)
            {
                return new List<StudentStatusDto>();
            }

            studentMacs = _db.Students
                .Where(s => s.TestName == test.Name)
                .Select(s => s.MacAddress)
                .ToList();

            onlineMacs = _db.Connections
                .Where(c => c.DisconnectedAt == null && c.Student!.TestName == test.Name)
                .Select(c => c.Student!.MacAddress)
                .ToHashSet();

            return _db.Students
                .Where(s => s.TestName == test.Name)
                .Select(s => new StudentStatusDto
                {
                    MacAddress = s.MacAddress,
                    Name = s.Name,
                    TestName = s.TestName,
                    IsOnline = onlineMacs.Contains(s.MacAddress)
                })
                .ToList();
        }
        else
        {
            onlineMacs = _db.Connections
                .Where(c => c.DisconnectedAt == null)
                .Select(c => c.Student!.MacAddress)
                .ToHashSet();

            return _db.Students
                .Select(s => new StudentStatusDto
                {
                    MacAddress = s.MacAddress,
                    Name = s.Name,
                    TestName = s.TestName,
                    IsOnline = onlineMacs.Contains(s.MacAddress)
                })
                .ToList();
        }
    }
}