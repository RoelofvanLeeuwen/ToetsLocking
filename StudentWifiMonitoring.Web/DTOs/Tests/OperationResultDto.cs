namespace StudentWifiMonitoring.Web.DTOs.Tests;

/// <summary>
/// Data transfer object voor operatieresultaten.
/// Geeft aan of een operatie geslaagd is en bevat eventuele foutmeldingen.
/// </summary>
public class OperationResultDto
{
    /// <summary>
    /// Geeft aan of de operatie succesvol was.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Foutmelding indien de operatie niet succesvol was.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
