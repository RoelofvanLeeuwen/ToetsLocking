namespace StudentWifiMonitoring.Web.DTOs.Export;

/// <summary>
/// Data transfer object voor het resultaat van een CSV-exportactie.
/// </summary>
public class ExportResultDto
{
    /// <summary>
    /// Geeft aan of de export succesvol was.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Bestandsnaam waaronder de export is opgeslagen.
    /// Gevuld als <see cref="Success"/> true is.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Foutmelding als de export mislukt is.
    /// Gevuld als <see cref="Success"/> false is.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
