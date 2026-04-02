using StudentWifiMonitoring.Web.DTOs.Tests;

namespace StudentWifiMonitoring.Web.Services.Interfaces;

/// <summary>
/// Interface voor het beheren van toetssessies.
/// Bevat alle businesslogica voor CRUD-operaties, filtering, paginering en statusbeheer van toetsen.
/// </summary>
public interface ITestManagementService
{
    /// <summary>
    /// Haalt een gefilterde en gepagineerde lijst van toetssessies op.
    /// Actieve toetsen worden bovenaan getoond, overige van nieuw naar oud.
    /// </summary>
    /// <param name="searchTerm">Optionele zoekterm voor toetsnaam (case-insensitive).</param>
    /// <param name="pageNumber">Paginanummer (1-based).</param>
    /// <param name="pageSize">Aantal items per pagina.</param>
    /// <returns>Gepagineerd resultaat met toetssessie DTO's en metadata.</returns>
    Task<PagedResultDto<TestSessionDto>> GetPagedTestsAsync(string? searchTerm, int pageNumber, int pageSize);

    /// <summary>
    /// Maakt een nieuwe toetssessie aan.
    /// Valideert de input en converteert lokale tijden naar UTC.
    /// </summary>
    /// <param name="createDto">DTO met gegevens voor de nieuwe toets.</param>
    /// <returns>Operatieresultaat met succes-indicatie en eventuele foutmelding.</returns>
    Task<OperationResultDto> CreateTestAsync(TestSessionCreateDto createDto);

    /// <summary>
    /// Werkt een bestaande toetssessie bij.
    /// Valideert de input en converteert lokale tijden naar UTC.
    /// </summary>
    /// <param name="updateDto">DTO met bijgewerkte gegevens van de toets.</param>
    /// <returns>Operatieresultaat met succes-indicatie en eventuele foutmelding.</returns>
    Task<OperationResultDto> UpdateTestAsync(TestSessionUpdateDto updateDto);

    /// <summary>
    /// Zet een actieve toetssessie inactief door de eindtijd te wijzigen.
    /// Sluit automatisch alle open verbindingen van studenten die voor deze toets geregistreerd zijn.
    /// </summary>
    /// <param name="testId">Unieke identifier van de toets die inactief gezet moet worden.</param>
    /// <returns>Resultaat met naam van de toets en aantal gedisconnecte studenten.</returns>
    Task<TestDeactivationResultDto> DeactivateTestAsync(int testId);
}
