namespace StudentWifiMonitoring.Web.DTOs.Tests;

/// <summary>
/// Data transfer object voor het bijwerken van een bestaande toetssessie.
/// </summary>
public class TestSessionUpdateDto
{
    /// <summary>
    /// Unieke identifier van de toetssessie die bijgewerkt wordt.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nieuwe naam van de toets.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nieuwe starttijd van de toets in lokale tijd.
    /// Wordt automatisch geconverteerd naar UTC door de service.
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// Nieuwe eindtijd van de toets in lokale tijd.
    /// Wordt automatisch geconverteerd naar UTC door de service.
    /// </summary>
    public DateTime EndTimeLocal { get; set; }
}
