namespace StudentWifiMonitoring.Web.DTOs.Students;

/// <summary>
/// Data transfer object voor een studentregistratie verzoek.
/// </summary>
public class StudentRegistrationRequestDto
{
    /// <summary>
    /// Naam van de student die zich wil registreren.
    /// </summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// IP-adres van de client.
    /// Wordt gebruikt om het MAC-adres te bepalen via de MAC resolver.
    /// </summary>
    public string ClientIpAddress { get; set; } = string.Empty;
}
