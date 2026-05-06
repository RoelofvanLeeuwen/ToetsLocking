using System.Diagnostics;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor het resolven van MAC-adressen op basis van IP-adressen.
/// </summary>
public interface IMacResolver
{
    /// <summary>
    /// Zoekt het MAC-adres dat hoort bij een gegeven IP-adres.
    /// </summary>
    /// <param name="ipAddress">Het IP-adres om op te zoeken.</param>
    /// <returns>Het MAC-adres in lowercase formaat, of null als het niet gevonden kan worden.</returns>
    string? GetMacForIp(string ipAddress);
}

/// <summary>
/// Linux-implementatie voor MAC-adres resolutie via het 'ip neigh' commando.
/// Gebruikt /usr/sbin/ip om de neighbor table uit te lezen.
/// </summary>
public class LinuxMacResolver : IMacResolver
{
    public string? GetMacForIp(string ipAddress)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/sbin/ip",
                ArgumentList = { "neigh", "show", ipAddress },
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var p = Process.Start(psi);
            if (p == null) return null;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            var parts = output.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var idx = Array.IndexOf(parts, "lladdr");
            if (idx > -1 && idx + 1 < parts.Length)
            {
                return parts[idx + 1].ToLowerInvariant();
            }
        }
        catch { }
        return null;
    }
}

/// <summary>
/// Windows-implementatie voor MAC-adres resolutie via het 'arp' commando.
/// Gebruikt arp -a om de ARP-tabel uit te lezen.
/// </summary>
public class WindowsMacResolver : IMacResolver
{
    public string? GetMacForIp(string ipAddress)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "arp",
                Arguments = "-a " + ipAddress,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using var p = Process.Start(psi);
            if (p == null) return null;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains(ipAddress))
                {
                    var cols = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (cols.Length >= 2)
                    {
                        return cols[1].Replace('-', ':').ToLowerInvariant();
                    }
                }
            }
        }
        catch { }
        return null;
    }
}
