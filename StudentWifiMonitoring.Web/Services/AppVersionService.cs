using System.Reflection;
using StudentWifiMonitoring.Web.Services.Interfaces;

namespace StudentWifiMonitoring.Web.Services;

public class AppVersionService : IAppVersionService
{
    public string Version { get; }

    public AppVersionService()
    {
        var fromEnv = Environment.GetEnvironmentVariable("APP_VERSION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            Version = fromEnv;
            return;
        }

        var fromAssembly = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        // SDK voegt commit-hash toe: "1.0.0+abc1234" → strip naar "1.0.0"
        Version = fromAssembly?.Split('+')[0] ?? "dev";
    }
}
