namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Representeert een WiFi-station met zijn MAC-adres.
/// </summary>
/// <param name="MacAddress">Het MAC-adres van het station in lowercase formaat.</param>
public record Station(string MacAddress);

/// <summary>
/// Service voor het ophalen van verbonden WiFi-stations.
/// </summary>
public interface IStationProvider
{
    /// <summary>
    /// Haalt een lijst op van alle momenteel verbonden WiFi-stations.
    /// </summary>
    /// <returns>Een read-only collectie van verbonden stations.</returns>
    Task<IReadOnlyCollection<Station>> GetStationsAsync();
}
