using StudentWifiMonitoring.Web.Services.Interfaces;
using StudentWifiMonitoring.Web.Services.Wifi;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Windows-mock voor IWifiManager. Simuleert drie netwerken voor development testing.
/// </summary>
public class MockWifiManager : IWifiManager
{
    private readonly object _lock = new();
    private string _connectedSsid = "GC-Docenten";

    private static readonly WifiNetwork[] Networks =
    [
        new("GC-Docenten",  90, "WPA2",  false),
        new("GC-Studenten", 75, "WPA2",  false),
        new("GC-Gasten",    50, "Open",  false),
    ];

    public async Task<IEnumerable<WifiNetwork>> ScanAsync(CancellationToken ct = default)
    {
        await Task.Delay(500, ct);
        lock (_lock)
        {
            return Networks.Select(n => n with { IsConnected = n.Ssid == _connectedSsid }).ToList();
        }
    }

    public async Task<ConnectResult> ConnectAsync(string ssid, string? password, CancellationToken ct = default)
    {
        await Task.Delay(800, ct);
        lock (_lock)
        {
            var network = Networks.FirstOrDefault(n => n.Ssid == ssid);
            if (network is null)
                return new ConnectResult(ConnectOutcome.NotFound);

            if (network.SecurityType != "Open" && password != "demo")
                return new ConnectResult(ConnectOutcome.AuthFailed, "Verkeerd wachtwoord (gebruik 'demo')");

            _connectedSsid = ssid;
            return new ConnectResult(ConnectOutcome.Success);
        }
    }

    public async Task<bool> DisconnectAsync(CancellationToken ct = default)
    {
        await Task.Delay(300, ct);
        lock (_lock)
        {
            _connectedSsid = string.Empty;
            return true;
        }
    }

    public Task<WifiStatus> GetStatusAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            var state = string.IsNullOrEmpty(_connectedSsid) ? WifiState.Disconnected : WifiState.Connected;
            var ip = state == WifiState.Connected ? "192.168.1.100" : null;
            return Task.FromResult(new WifiStatus(_connectedSsid.NullIfEmpty(), ip, state));
        }
    }
}

file static class StringExtensions
{
    internal static string? NullIfEmpty(this string s) => string.IsNullOrEmpty(s) ? null : s;
}
