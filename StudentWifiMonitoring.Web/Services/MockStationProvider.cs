namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Mock-implementatie van IStationProvider voor lokale ontwikkeling op Windows.
/// Biedt een bestuurbare lijst van mock MAC-adressen voor acceptatietests.
/// </summary>
/// <remarks>
/// Deze provider is bedoeld voor development-only testing van de volledige monitoring flow:
/// MockStationProvider -> MonitoringService -> database -> SignalR -> dashboard.
/// De interne lijst is thread-safe en kan runtime aangepast worden via Add/Remove methodes.
/// </remarks>
public class MockStationProvider : IStationProvider
{
    private readonly HashSet<string> _mockStations = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initialiseert de provider met twee standaard mock MAC-adressen.
    /// </summary>
    public MockStationProvider()
    {
        // Start met twee standaard mock stations voor directe testbaarheid
        _mockStations.Add("aa:bb:cc:dd:ee:01");
        _mockStations.Add("aa:bb:cc:dd:ee:02");
    }

    /// <summary>
    /// Haalt de actuele lijst van mock stations op.
    /// </summary>
    /// <returns>Een read-only collectie van de huidige mock stations.</returns>
    public Task<IReadOnlyCollection<Station>> GetStationsAsync()
    {
        lock (_lock)
        {
            var stations = _mockStations
                .Select(mac => new Station(mac))
                .ToList();
            return Task.FromResult<IReadOnlyCollection<Station>>(stations);
        }
    }

    /// <summary>
    /// Voegt een mock MAC-adres toe aan de lijst van verbonden stations.
    /// Simuleert het connecten van een WiFi-client.
    /// </summary>
    /// <param name="macAddress">Het MAC-adres in lowercase formaat (bijv. "aa:bb:cc:dd:ee:03").</param>
    /// <returns>True als het MAC-adres toegevoegd is, false als deze al bestond.</returns>
    public bool AddStation(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return false;

        lock (_lock)
        {
            return _mockStations.Add(macAddress.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Verwijdert een mock MAC-adres uit de lijst van verbonden stations.
    /// Simuleert het disconnecten van een WiFi-client.
    /// </summary>
    /// <param name="macAddress">Het MAC-adres om te verwijderen.</param>
    /// <returns>True als het MAC-adres verwijderd is, false als deze niet bestond.</returns>
    public bool RemoveStation(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return false;

        lock (_lock)
        {
            return _mockStations.Remove(macAddress.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Verwijdert alle mock stations uit de lijst.
    /// </summary>
    public void ClearStations()
    {
        lock (_lock)
        {
            _mockStations.Clear();
        }
    }

    /// <summary>
    /// Haalt het aantal momenteel verbonden mock stations op.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _mockStations.Count;
            }
        }
    }
}
