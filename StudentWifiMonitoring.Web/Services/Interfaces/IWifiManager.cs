using StudentWifiMonitoring.Web.Services.Wifi;

namespace StudentWifiMonitoring.Web.Services.Interfaces;

public interface IWifiManager
{
    Task<IEnumerable<WifiNetwork>> ScanAsync(CancellationToken ct = default);
    Task<ConnectResult> ConnectAsync(string ssid, string? password, CancellationToken ct = default);
    Task<bool> DisconnectAsync(CancellationToken ct = default);
    Task<WifiStatus> GetStatusAsync(CancellationToken ct = default);
}
