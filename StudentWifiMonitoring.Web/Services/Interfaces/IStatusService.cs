using StudentWifiMonitoring.Web.DTOs.Status;

namespace StudentWifiMonitoring.Web.Services.Interfaces;

/// <summary>
/// Interface voor het ophalen van de huidige verbindingsstatus van studenten.
/// </summary>
public interface IStatusService
{
    /// <summary>
    /// Haalt een lijst op van studenten die momenteel verbonden zijn.
    /// Een student is verbonden als er een open verbinding bestaat zonder DisconnectedAt-waarde.
    /// </summary>
    /// <returns>Lijst van DTO's met naam en MAC-adres van verbonden studenten.</returns>
    List<ConnectedStudentDto> GetConnectedStudents();
}
