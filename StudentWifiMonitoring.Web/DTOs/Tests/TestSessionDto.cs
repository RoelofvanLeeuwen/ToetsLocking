namespace StudentWifiMonitoring.Web.DTOs.Tests;

/// <summary>
/// Data transfer object voor een toetssessie.
/// Wordt gebruikt voor lijstweergave en bewerken van toetsen.
/// </summary>
public class TestSessionDto
{
    /// <summary>
    /// Unieke identifier van de toetssessie.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Naam van de toets.
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

    /// <summary>
    /// Geeft aan of de toets momenteel actief is.
    /// Een toets is actief als de huidige tijd tussen start- en eindtijd valt.
    /// </summary>
    public bool IsActive { get; set; }
}
