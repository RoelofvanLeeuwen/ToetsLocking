using StudentWifiMonitoring.Web.DTOs.Dashboard;
using StudentWifiMonitoring.Web.DTOs.Tests;

namespace StudentWifiMonitoring.Web.Services.Interfaces;

/// <summary>
/// Interface voor het ophalen van dashboard data zoals toetssessies en studentstatus.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Haalt alle toetssessies op als DTO's, gesorteerd op starttijd (nieuwste eerst).
    /// </summary>
    /// <returns>Lijst van toetssessie DTO's.</returns>
    List<TestSessionDto> GetTestSessions();

    /// <summary>
    /// Haalt studenten met hun online status op, optioneel gefilterd op een toetssessie.
    /// </summary>
    /// <param name="testSessionId">Optioneel: ID van de toetssessie om op te filteren.</param>
    /// <returns>Lijst van student status DTO's inclusief online indicator.</returns>
    List<StudentStatusDto> GetStudentsWithStatus(int? testSessionId);

    /// <summary>
    /// Haalt de volledige activiteitenlog op van een student, optioneel gefilterd op een toetssessie.
    /// </summary>
    List<StudentActivityDto> GetStudentActivity(string macAddress, int? testSessionId);
}
