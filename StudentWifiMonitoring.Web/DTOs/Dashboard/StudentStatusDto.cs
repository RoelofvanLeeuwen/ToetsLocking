namespace StudentWifiMonitoring.Web.DTOs.Dashboard;

/// <summary>
/// Data transfer object voor een student met online status op het dashboard.
/// </summary>
public class StudentStatusDto
{
    /// <summary>
    /// MAC-adres van de student.
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// Naam van de student.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Naam van de toets waarvoor de student is geregistreerd.
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// Geeft aan of de student momenteel verbonden is.
    /// </summary>
    public bool IsOnline { get; set; }
}
