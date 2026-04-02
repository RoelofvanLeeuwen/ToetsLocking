namespace StudentWifiMonitoring.Web.DTOs.Tests;

/// <summary>
/// Data transfer object voor het resultaat van het inactief zetten van een toets.
/// </summary>
public class TestDeactivationResultDto
{
    /// <summary>
    /// Naam van de toets die inactief is gezet.
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// Aantal studenten dat automatisch gedisconnect is.
    /// </summary>
    public int DisconnectedStudentsCount { get; set; }
}
