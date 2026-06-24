using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Domain;
using StudentWifiMonitoring.Web.Services.Interfaces;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor docent-authenticatie op basis van een geconfigureerde pincode.
/// De docentstatus wordt uitgelezen uit een HttpOnly cookie dat door het login-endpoint
/// in Program.cs wordt gezet. Schrijfacties (login/logout) verlopen via HTTP POST-endpoints,
/// zodat het cookie vóór het verzenden van de response gezet kan worden.
/// Dit voorkomt de InvalidOperationException die optreedt wanneer session of cookie-writes
/// vanuit een interactieve Blazor Server component worden geprobeerd.
/// De actieve pincode komt uit de database als die daar aanwezig is; anders uit de configuratie.
/// </summary>
public class TeacherAuthService : ITeacherAuthService
{
    /// <summary>Naam van het authenticatiecookie.</summary>
    internal const string CookieName = "teacher_auth";

    /// <summary>Verwachte waarde van het authenticatiecookie bij een ingelogde docent.</summary>
    internal const string CookieValue = "1";

    /// <summary>Sleutel waarmee de pincode in de AppSettings-tabel wordt opgeslagen.</summary>
    internal const string PasswordSettingKey = "Teacher:Password";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public TeacherAuthService(IHttpContextAccessor httpContextAccessor, AppDbContext db, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public bool IsTeacher()
    {
        var cookie = _httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        return cookie == CookieValue;
    }

    /// <inheritdoc />
    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var activePassword = await GetActivePasswordAsync();

        if (currentPassword != activePassword)
            return false;

        var setting = await _db.AppSettings.FindAsync(PasswordSettingKey);
        if (setting is null)
        {
            _db.AppSettings.Add(new AppSetting { Key = PasswordSettingKey, Value = newPassword });
        }
        else
        {
            setting.Value = newPassword;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<string> GetActivePasswordAsync()
    {
        var dbPassword = await _db.AppSettings
            .Where(s => s.Key == PasswordSettingKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        return dbPassword ?? _configuration["Teacher:Password"] ?? string.Empty;
    }
}
