namespace StudentWifiMonitoring.Web.DTOs.MyScreen;

/// <summary>
/// Data transfer object voor de verbindingsstatus van een individuele student.
/// Wordt gebruikt op de MyScreen-pagina om de eigen status te tonen.
/// </summary>
public class StudentConnectionStatusDto
{
    /// <summary>
    /// Naam van de student. Null als de student niet geregistreerd is.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Naam van de toets waarvoor de student geregistreerd is. Null als de student niet geregistreerd is.
    /// </summary>
    public string? TestName { get; set; }

    /// <summary>
    /// Geeft aan of de student geregistreerd is in het systeem.
    /// </summary>
    public bool IsRegistered { get; set; }

    /// <summary>
    /// Geeft aan of de student momenteel een actieve verbinding heeft.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Geeft aan of de student op "Ik ben klaar" heeft geklikt.
    /// </summary>
    public bool IsTestComplete { get; set; }
}
