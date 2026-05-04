namespace StudentWifiMonitoring.Web.DTOs.Status;

/// <summary>
/// Data transfer object voor een student die momenteel verbonden is.
/// Wordt gebruikt op de statuspagina voor weergave van actieve verbindingen.
/// </summary>
public class ConnectedStudentDto
{
    /// <summary>
    /// Naam van de student.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// MAC-adres van het apparaat van de student.
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;
}
