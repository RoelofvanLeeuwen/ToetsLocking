using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Domain;
using StudentWifiMonitoring.Web.Services.Interfaces;

namespace StudentWifiMonitoring.Web.Services;

public class AccessPointSettingsService : IAccessPointSettingsService
{
    public const string SsidKey     = "AccessPoint.Ssid";
    public const string PasswordKey = "AccessPoint.Password";

    private readonly AppDbContext _db;

    public AccessPointSettingsService(AppDbContext db) => _db = db;

    public async Task<(string Ssid, string? Password)> GetAsync(CancellationToken ct = default)
    {
        var rows = await _db.AppSettings
            .Where(s => s.Key == SsidKey || s.Key == PasswordKey)
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        rows.TryGetValue(SsidKey, out var ssid);
        rows.TryGetValue(PasswordKey, out var pwd);
        return (ssid ?? string.Empty, string.IsNullOrEmpty(pwd) ? null : pwd);
    }

    public async Task SaveAsync(string ssid, string? password, CancellationToken ct = default)
    {
        await UpsertAsync(SsidKey, ssid, ct);
        await UpsertAsync(PasswordKey, password ?? string.Empty, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertAsync(string key, string value, CancellationToken ct)
    {
        var row = await _db.AppSettings.FindAsync([key], ct);
        if (row is null)
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        else
            row.Value = value;
    }
}
