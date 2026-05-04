using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.DTOs.Export;
using StudentWifiMonitoring.Web.Services.Interfaces;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor het exporteren van event-data naar CSV-bestanden.
/// Gebruikt de bestaande CsvExporter voor de daadwerkelijke CSV-generatie.
/// </summary>
public class ExportService : IExportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ExportService> _logger;

    public ExportService(AppDbContext db, ILogger<ExportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Exporteert alle events naar een CSV-bestand op de server.
    /// De bestandsnaam bevat een tijdstempel en wordt teruggegeven in het resultaat.
    /// </summary>
    public async Task<ExportResultDto> ExportAllAsync()
    {
        try
        {
            var csv = await CsvExporter.ExportAllAsync(_db);
            var fileName = $"events-all-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            await File.WriteAllTextAsync(fileName, csv);

            _logger.LogInformation("Export succesvol opgeslagen als {FileName}", fileName);

            return new ExportResultDto
            {
                Success = true,
                FileName = fileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij exporteren van alle events");
            return new ExportResultDto
            {
                Success = false,
                ErrorMessage = "Er is een fout opgetreden bij het exporteren."
            };
        }
    }
}
