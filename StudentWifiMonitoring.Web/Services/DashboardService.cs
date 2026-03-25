using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Domain;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor het beheren van dashboard data zoals studenten, toetssessies en online status.
/// Bevat de businesslogica voor het filteren en ophalen van student- en connectiegegevens.
/// </summary>
public class DashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Haalt alle toetssessies op, gesorteerd op starttijd (nieuwste eerst).
    /// </summary>
    public List<TestSession> GetTestSessions()
    {
        return _db.TestSessions.OrderByDescending(t => t.StartTime).ToList();
    }

    /// <summary>
    /// Haalt studenten en hun online status op, optioneel gefilterd op een specifieke toetssessie.
    /// </summary>
    /// <param name="testSessionId">Optioneel: ID van de toetssessie om op te filteren.</param>
    /// <returns>Een tuple met de lijst studenten en een set van online MAC-adressen.</returns>
    public (List<Student> Students, HashSet<string> OnlineMacs) GetStudentsWithStatus(int? testSessionId)
    {
        List<Student> students;
        HashSet<string> onlineMacs;

        if (testSessionId.HasValue)
        {
            var test = _db.TestSessions.Find(testSessionId.Value);
            if (test != null)
            {
                students = _db.Students.Where(s => s.TestName == test.Name).ToList();
                onlineMacs = _db.Connections
                    .Where(c => c.DisconnectedAt == null && c.Student!.TestName == test.Name)
                    .Select(c => c.Student!.MacAddress)
                    .ToHashSet();
            }
            else
            {
                students = new List<Student>();
                onlineMacs = new HashSet<string>();
            }
        }
        else
        {
            students = _db.Students.ToList();
            onlineMacs = _db.Connections
                .Where(c => c.DisconnectedAt == null)
                .Select(c => c.Student!.MacAddress)
                .ToHashSet();
        }

        return (students, onlineMacs);
    }
}