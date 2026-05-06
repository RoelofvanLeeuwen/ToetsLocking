using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.DTOs.Status;
using StudentWifiMonitoring.Web.Services.Interfaces;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor het ophalen van de huidige verbindingsstatus van studenten.
/// Bevat de query- en mappinglogica voor actieve verbindingen.
/// </summary>
public class StatusService : IStatusService
{
    private readonly AppDbContext _db;

    public StatusService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Haalt een lijst op van studenten die momenteel verbonden zijn.
    /// Een student is verbonden als er een open verbinding bestaat zonder DisconnectedAt-waarde.
    /// </summary>
    public List<ConnectedStudentDto> GetConnectedStudents()
    {
        return _db.Connections
            .Where(c => c.DisconnectedAt == null && c.Student != null)
            .Select(c => c.Student!)
            .Distinct()
            .Select(s => new ConnectedStudentDto
            {
                Name = s.Name,
                MacAddress = s.MacAddress
            })
            .ToList();
    }
}
