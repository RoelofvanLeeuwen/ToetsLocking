namespace StudentWifiMonitoring.Web.DTOs.Students;

/// <summary>
/// Data transfer object voor het resultaat van een studentregistratie.
/// </summary>
public class StudentRegistrationResultDto
{
    /// <summary>
    /// Geeft aan of de registratie succesvol was.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Foutmelding indien de registratie niet succesvol was.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Succesbericht voor succesvolle registratie.
    /// </summary>
    public string? SuccessMessage { get; set; }
}
