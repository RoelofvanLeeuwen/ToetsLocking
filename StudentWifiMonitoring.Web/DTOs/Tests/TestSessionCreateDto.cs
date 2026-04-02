namespace StudentWifiMonitoring.Web.DTOs.Tests;

/// <summary>
/// Data transfer object voor het aanmaken van een nieuwe toetssessie.
/// </summary>
public class TestSessionCreateDto
{
    /// <summary>
    /// Naam van de toets.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Starttijd van de toets in lokale tijd.
    /// Wordt automatisch geconverteerd naar UTC door de service.
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// Eindtijd van de toets in lokale tijd.
    /// Wordt automatisch geconverteerd naar UTC door de service.
    /// </summary>
    public DateTime EndTimeLocal { get; set; }
}
