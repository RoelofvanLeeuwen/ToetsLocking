namespace StudentWifiMonitoring.Web.DTOs.Students;

/// <summary>
/// Data transfer object voor een actieve toetssessie.
/// Wordt gebruikt om de actieve toets weer te geven op de registratiepagina.
/// </summary>
public class ActiveTestSessionDto
{
    public int Id { get; set; }

    /// <summary>
    /// Naam van de actieve toets.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Starttijd van de toets (UTC).
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Eindtijd van de toets (UTC).
    /// </summary>
    public DateTime EndTime { get; set; }
}
