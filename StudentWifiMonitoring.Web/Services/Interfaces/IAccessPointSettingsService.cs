namespace StudentWifiMonitoring.Web.Services.Interfaces;

public interface IAccessPointSettingsService
{
    Task<(string Ssid, string? Password)> GetAsync(CancellationToken ct = default);
    Task SaveAsync(string ssid, string? password, CancellationToken ct = default);
}
