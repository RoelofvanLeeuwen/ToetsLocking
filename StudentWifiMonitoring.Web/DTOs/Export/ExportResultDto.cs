namespace StudentWifiMonitoring.Web.DTOs.Export;

/// <summary>
/// Data transfer object voor het resultaat van een CSV-exportactie.
/// De CSV-inhoud wordt in memory teruggegeven zodat de browser de download direct kan starten.
/// </summary>
public class ExportResultDto
{
    /// <summary>
    /// Geeft aan of de export succesvol was.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Voorgestelde bestandsnaam voor de download, inclusief tijdstempel.
    /// Gevuld als <see cref="Success"/> true is.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Inhoud van het CSV-bestand als string.
    /// Gevuld als <see cref="Success"/> true is.
    /// Wordt door de pagina via JS Interop als browserdownload aangeboden.
    /// </summary>
    public string? CsvContent { get; set; }

    /// <summary>
    /// Foutmelding als de export mislukt is.
    /// Gevuld als <see cref="Success"/> false is.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
