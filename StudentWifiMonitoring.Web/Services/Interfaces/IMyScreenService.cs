using StudentWifiMonitoring.Web.DTOs.MyScreen;

namespace StudentWifiMonitoring.Web.Services.Interfaces;

/// <summary>
/// Interface voor het ophalen van de verbindingsstatus van een individuele student op basis van MAC-adres.
/// Bevat de databaselogica voor de MyScreen-pagina.
/// </summary>
public interface IMyScreenService
{
    /// <summary>
    /// Haalt de verbindingsstatus op van de student met het opgegeven MAC-adres.
    /// Geeft de registratiestatus, naam, toetsnaam en actieve verbindingsstatus terug.
    /// </summary>
    /// <param name="macAddress">MAC-adres van het apparaat van de student.</param>
    /// <returns>
    /// DTO met verbindingsstatus. <see cref="StudentConnectionStatusDto.IsRegistered"/> is false
    /// als er geen student gevonden is met het opgegeven MAC-adres.
    /// </returns>
    Task<StudentConnectionStatusDto> GetStudentConnectionStatusAsync(string macAddress);

    /// <summary>
    /// Markeert de student als "klaar" met de toets: sluit open verbindingen en stuurt een SignalR-event.
    /// </summary>
    Task MarkTestCompleteAsync(string macAddress);
}
