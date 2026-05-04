using StudentWifiMonitoring.Web.DTOs.Export;
using StudentWifiMonitoring.Web.DTOs.Tests;

namespace StudentWifiMonitoring.Web.Services.Interfaces;

/// <summary>
/// Interface voor het exporteren van event-data naar CSV-bestanden.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exporteert alle events naar een CSV-bestand op de server.
    /// De bestandsnaam bevat een tijdstempel en wordt teruggegeven in het resultaat.
    /// </summary>
    /// <returns>Resultaat met succes-indicatie en bestandsnaam of foutmelding.</returns>
    Task<ExportResultDto> ExportAllAsync();

    /// <summary>
    /// Haalt alle toetssessies op als DTO's voor gebruik in de exportpagina, gesorteerd op starttijd (nieuwste eerst).
    /// </summary>
    /// <returns>Lijst van toetssessie DTO's.</returns>
    List<TestSessionDto> GetTestSessions();

    /// <summary>
    /// Exporteert alle events van een specifieke toetssessie naar een CSV-bestand op de server.
    /// De bestandsnaam bevat het toets-ID en een tijdstempel en wordt teruggegeven in het resultaat.
    /// </summary>
    /// <param name="testSessionId">ID van de toetssessie om te exporteren.</param>
    /// <returns>Resultaat met succes-indicatie en bestandsnaam of foutmelding.</returns>
    Task<ExportResultDto> ExportByTestAsync(int testSessionId);
}
