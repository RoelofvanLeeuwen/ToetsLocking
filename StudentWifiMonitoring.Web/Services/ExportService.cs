using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.DTOs.Export;
using StudentWifiMonitoring.Web.DTOs.Tests;
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
    /// Genereert een CSV-export van alle events in memory.
    /// De inhoud wordt teruggegeven in de DTO zodat de browser de download direct kan starten.
    /// Er worden geen bestanden op de server opgeslagen.
    /// </summary>
    public async Task<ExportResultDto> ExportAllAsync()
    {
        try
        {
            var csv = await CsvExporter.ExportAllAsync(_db);
            var fileName = $"events-all-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

            _logger.LogInformation("Export gegenereerd: {FileName}", fileName);

            return new ExportResultDto
            {
                Success = true,
                FileName = fileName,
                CsvContent = csv
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

    /// <summary>
    /// Haalt alle toetssessies op als DTO's voor gebruik in de exportpagina, gesorteerd op starttijd (nieuwste eerst).
    /// </summary>
    public List<TestSessionDto> GetTestSessions()
    {
        return _db.TestSessions
            .OrderByDescending(t => t.StartTime)
            .Select(t => new TestSessionDto
            {
                Id = t.Id,
                Name = t.Name,
                StartTime = t.StartTime,
                EndTime = t.EndTime
            })
            .ToList();
    }

    /// <summary>
    /// Genereert een CSV-export van alle events van een specifieke toetssessie in memory.
    /// De inhoud wordt teruggegeven in de DTO zodat de browser de download direct kan starten.
    /// Er worden geen bestanden op de server opgeslagen.
    /// </summary>
    public async Task<ExportResultDto> ExportByTestAsync(int testSessionId)
    {
        try
        {
            var csv = await CsvExporter.ExportByTestAsync(_db, testSessionId);
            var fileName = $"events-test-{testSessionId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

            _logger.LogInformation("Export per toets gegenereerd: {FileName}", fileName);

            return new ExportResultDto
            {
                Success = true,
                FileName = fileName,
                CsvContent = csv
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij exporteren van events voor toets {TestSessionId}", testSessionId);
            return new ExportResultDto
            {
                Success = false,
                ErrorMessage = "Er is een fout opgetreden bij het exporteren."
            };
        }
    }
}
