using StudentWifiMonitoring.Web.DTOs.Export;

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
}
