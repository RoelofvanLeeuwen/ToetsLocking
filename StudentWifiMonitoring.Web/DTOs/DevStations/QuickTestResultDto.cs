namespace StudentWifiMonitoring.Web.DTOs.DevStations;

/// <summary>
/// Data transfer object voor het resultaat van een snelle teststudent-aanmaak op de DevStations-pagina.
/// </summary>
public class QuickTestResultDto
{
    /// <summary>
    /// Geeft aan of de operatie succesvol was.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Beschrijvend bericht over het resultaat, geschikt voor weergave aan de gebruiker.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
