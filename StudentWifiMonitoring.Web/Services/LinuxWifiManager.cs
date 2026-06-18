using System.Diagnostics;
using System.Text.RegularExpressions;
using StudentWifiMonitoring.Web.Services.Interfaces;
using StudentWifiMonitoring.Web.Services.Wifi;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Linux-implementatie voor WiFi-beheer via nmcli op de Raspberry Pi.
/// De Docker-container draait als root — sudo is niet nodig.
///</summary>
public class LinuxWifiManager : IWifiManager
{
    private readonly string _iface;
    private readonly ILogger<LinuxWifiManager> _logger;

    // Splits op ':' maar niet op '\:' (nmcli terse-mode escaping)
    private static readonly Regex SplitColon = new(@"(?<!\\):", RegexOptions.Compiled);

    public LinuxWifiManager(IConfiguration cfg, ILogger<LinuxWifiManager> logger)
    {
        _iface = cfg["Monitoring:Interface"] ?? "wlan0";
        _logger = logger;
    }

    public async Task<IEnumerable<WifiNetwork>> ScanAsync(CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        var output = await RunAsync("nmcli", ["-t", "-f", "SSID,SIGNAL,SECURITY", "device", "wifi", "list", "--rescan", "yes"], cts.Token);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var networks = new List<WifiNetwork>();

        var status = await GetStatusAsync(ct);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = SplitColon.Split(line);
            if (parts.Length < 2) continue;

            var ssid = parts[0].Replace(@"\:", ":");
            if (string.IsNullOrWhiteSpace(ssid) || !seen.Add(ssid)) continue;

            int.TryParse(parts[1], out var signal);
            var security = parts.Length >= 3 ? parts[2].Trim() : string.Empty;
            var secType = string.IsNullOrWhiteSpace(security) || security == "--" ? "Open" : "WPA2";

            networks.Add(new WifiNetwork(ssid, signal, secType, ssid == status.ConnectedSsid));
        }

        return networks.OrderByDescending(n => n.SignalStrength);
    }

    public async Task<ConnectResult> ConnectAsync(string ssid, string? password, CancellationToken ct = default)
    {
        _logger.LogInformation("Verbinden met netwerk {Ssid}", ssid);

        var args = new List<string> { "device", "wifi", "connect", ssid };
        if (!string.IsNullOrEmpty(password))
        {
            args.Add("password");
            args.Add(password);
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var (stdout, stderr) = await RunWithStderrAsync("nmcli", [.. args], cts.Token);

            if (stderr.Contains("Secrets were required", StringComparison.OrdinalIgnoreCase) ||
                stderr.Contains("802-11-wireless-security.psk", StringComparison.OrdinalIgnoreCase))
                return new ConnectResult(ConnectOutcome.AuthFailed, "Verificatie mislukt — controleer het wachtwoord.");

            if (stderr.Contains("No network with SSID", StringComparison.OrdinalIgnoreCase))
                return new ConnectResult(ConnectOutcome.NotFound, $"Netwerk '{ssid}' niet gevonden.");

            if (stderr.Contains("not authorized", StringComparison.OrdinalIgnoreCase) ||
                stderr.Contains("permission", StringComparison.OrdinalIgnoreCase))
                return new ConnectResult(ConnectOutcome.PermissionDenied, "Geen rechten om nmcli uit te voeren.");

            if (!string.IsNullOrWhiteSpace(stderr))
                return new ConnectResult(ConnectOutcome.OsError, stderr.Trim());

            return new ConnectResult(ConnectOutcome.Success);
        }
        catch (OperationCanceledException)
        {
            return new ConnectResult(ConnectOutcome.Timeout, "Verbinding duurde te lang.");
        }
        finally
        {
            // Wachtwoord nooit bewaren — wis lokale variabele
            password = null;
        }
    }

    public async Task<bool> DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            await RunAsync("nmcli", ["device", "disconnect", _iface], ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verbreken van WiFi-verbinding mislukt");
            return false;
        }
    }

    public async Task<WifiStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var deviceOutput = await RunAsync("nmcli", ["-t", "-f", "DEVICE,STATE,CONNECTION", "device", "status"], ct);

        string? connectedSsid = null;
        var state = WifiState.Disconnected;

        foreach (var line in deviceOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = SplitColon.Split(line);
            if (parts.Length < 3) continue;
            if (!parts[0].Equals(_iface, StringComparison.OrdinalIgnoreCase)) continue;

            state = parts[1] switch
            {
                "connected"    => WifiState.Connected,
                "connecting"   => WifiState.Connecting,
                _              => WifiState.Disconnected,
            };

            if (state == WifiState.Connected)
                connectedSsid = parts[2].Replace(@"\:", ":").Trim();

            break;
        }

        string? ip = null;
        if (state == WifiState.Connected)
        {
            var ipOutput = await RunAsync("nmcli", ["-t", "-f", "IP4.ADDRESS", "device", "show", _iface], ct);
            var ipLine = ipOutput.Split('\n').FirstOrDefault(l => l.StartsWith("IP4.ADDRESS"));
            if (ipLine is not null)
            {
                var ipParts = SplitColon.Split(ipLine);
                if (ipParts.Length >= 2)
                    ip = ipParts[1].Split('/')[0].Trim();
            }
        }

        return new WifiStatus(connectedSsid, ip, state);
    }

    private static async Task<string> RunAsync(string fileName, string[] args, CancellationToken ct = default)
    {
        var (stdout, _) = await RunWithStderrAsync(fileName, args, ct);
        return stdout;
    }

    private static async Task<(string Stdout, string Stderr)> RunWithStderrAsync(string fileName, string[] args, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Kon '{fileName}' niet starten");
        var stdoutTask = p.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);

        return (await stdoutTask, await stderrTask);
    }
}
