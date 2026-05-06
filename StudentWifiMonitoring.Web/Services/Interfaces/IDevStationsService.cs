using StudentWifiMonitoring.Web.DTOs.DevStations;
using StudentWifiMonitoring.Web.DTOs.Students;

namespace StudentWifiMonitoring.Web.Services.Interfaces;

/// <summary>
/// Interface voor development-only operaties op de DevStations-pagina.
/// Bevat logica voor het ophalen van de actieve toets en het snel aanmaken van teststudenten.
/// </summary>
public interface IDevStationsService
{
    /// <summary>
    /// Haalt de huidige actieve toetssessie op.
    /// Een toets is actief als de huidige tijd tussen start- en eindtijd valt.
    /// </summary>
    /// <returns>De actieve toets als DTO, of null als er geen actieve toets is.</returns>
    Task<ActiveTestSessionDto?> GetActiveTestSessionAsync();

    /// <summary>
    /// Maakt een teststudent aan of werkt een bestaande student bij op basis van MAC-adres.
    /// Koppelt de student aan de actieve toetssessie.
    /// </summary>
    /// <param name="studentName">De naam van de teststudent.</param>
    /// <param name="macAddress">Het MAC-adres van de teststudent (lowercase formaat).</param>
    /// <returns>Resultaat met succes-indicatie, bericht en de toetsnaam van de actieve toets.</returns>
    Task<QuickTestResultDto> UpsertTestStudentAsync(string studentName, string macAddress);
}
