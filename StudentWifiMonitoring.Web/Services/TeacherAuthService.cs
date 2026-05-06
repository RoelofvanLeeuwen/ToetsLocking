using StudentWifiMonitoring.Web.Services.Interfaces;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor docent-authenticatie op basis van een geconfigureerde pincode.
/// De docentstatus wordt uitgelezen uit een HttpOnly cookie dat door het login-endpoint
/// in Program.cs wordt gezet. Schrijfacties (login/logout) verlopen via HTTP POST-endpoints,
/// zodat het cookie vóór het verzenden van de response gezet kan worden.
/// Dit voorkomt de InvalidOperationException die optreedt wanneer session of cookie-writes
/// vanuit een interactieve Blazor Server component worden geprobeerd.
/// </summary>
public class TeacherAuthService : ITeacherAuthService
{
    /// <summary>Naam van het authenticatiecookie.</summary>
    internal const string CookieName = "teacher_auth";

    /// <summary>Verwachte waarde van het authenticatiecookie bij een ingelogde docent.</summary>
    internal const string CookieValue = "1";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TeacherAuthService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public bool IsTeacher()
    {
        var cookie = _httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        return cookie == CookieValue;
    }
}
