using System.Diagnostics;

namespace StudentWifiMonitoring.Web.Services
{
    public interface IMacResolver
    {
        string? GetMacForIp(string ipAddress);
    }

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
}
