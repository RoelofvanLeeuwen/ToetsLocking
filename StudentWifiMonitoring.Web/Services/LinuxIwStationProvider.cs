using System.Diagnostics;
using System.Text.RegularExpressions;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Linux-implementatie voor het ophalen van verbonden WiFi-stations via het 'iw' commando.
/// Gebruikt /usr/sbin/iw om de station dump uit te lezen van een specifieke netwerkinterface.
/// </summary>
public class LinuxIwStationProvider : IStationProvider
{
    private readonly string _iface;
    private static readonly Regex MacRegex = new("station\\s+([0-9a-f:]{17})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public LinuxIwStationProvider(IConfiguration cfg)
    {
        _iface = cfg["Monitoring:Interface"] ?? "wlan0";
    }

    public async Task<IReadOnlyCollection<Station>> GetStationsAsync()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/usr/sbin/iw",
            ArgumentList = { "dev", _iface, "station", "dump" },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start iw");
        var output = await p.StandardOutput.ReadToEndAsync();
        await p.WaitForExitAsync();

        var stations = new List<Station>();
        foreach (Match m in MacRegex.Matches(output))
        {
            stations.Add(new Station(m.Groups[1].Value.ToLowerInvariant()));
        }
        return stations;
    }
}
