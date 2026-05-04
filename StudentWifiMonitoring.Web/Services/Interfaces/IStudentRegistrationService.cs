using StudentWifiMonitoring.Web.DTOs.Students;

namespace StudentWifiMonitoring.Web.Services.Interfaces;

/// <summary>
/// Interface voor het registreren van studenten voor toetssessies.
/// Bevat businesslogica voor het ophalen van de actieve toets en het registreren van studenten.
/// </summary>
public interface IStudentRegistrationService
{
    /// <summary>
    /// Haalt de huidige actieve toetssessie op.
    /// Een toets is actief als de huidige tijd tussen start- en eindtijd valt.
    /// </summary>
    /// <returns>De actieve toets als DTO, of null als er geen actieve toets is.</returns>
    Task<ActiveTestSessionDto?> GetActiveTestSessionAsync();

    /// <summary>
    /// Registreert een student voor de actieve toetssessie.
    /// Bepaalt automatisch het MAC-adres op basis van het client IP-adres via de MAC resolver.
    /// Voert een upsert uit: nieuwe student wordt toegevoegd, bestaande student (op basis van MAC) wordt bijgewerkt.
    /// </summary>
    /// <param name="request">Registratieverzoek met studentnaam en client IP-adres.</param>
    /// <returns>Resultaat met succes-indicatie en eventuele fout- of succesbericht.</returns>
    Task<StudentRegistrationResultDto> RegisterStudentAsync(StudentRegistrationRequestDto request);
}
