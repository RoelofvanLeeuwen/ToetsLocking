using StudentWifiMonitoring.Web.DTOs.Export;
using StudentWifiMonitoring.Web.DTOs.Tests;

namespace StudentWifiMonitoring.Web.Services.Interfaces;

/// <summary>
/// Interface voor het exporteren van event-data naar CSV-bestanden.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Haalt alle toetssessies op als DTO's voor gebruik in de exportpagina, gesorteerd op starttijd (nieuwste eerst).
    /// </summary>
    /// <returns>Lijst van toetssessie DTO's.</returns>
    List<TestSessionDto> GetTestSessions();

    /// <summary>
    /// Genereert een CSV-export van alle events van een specifieke toetssessie in memory.
    /// De inhoud wordt teruggegeven in de DTO zodat de browser de download direct kan starten.
    /// Er worden geen bestanden op de server opgeslagen.
    /// </summary>
    /// <param name="testSessionId">ID van de toetssessie om te exporteren.</param>
    /// <returns>Resultaat met succes-indicatie, bestandsnaam, CSV-inhoud of foutmelding.</returns>
    Task<ExportResultDto> ExportByTestAsync(int testSessionId);
}
