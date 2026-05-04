namespace StudentWifiMonitoring.Web.Services.Interfaces;

/// <summary>
/// Definieert de contract voor docent-authenticatie op basis van een HttpOnly cookie.
/// Login en logout verlopen via HTTP POST-endpoints zodat het cookie vóór
/// het verzenden van de response gezet kan worden, wat vereist is door ASP.NET Core.
/// </summary>
public interface ITeacherAuthService
{
    /// <summary>
    /// Controleert of de huidige gebruiker ingelogd is als docent op basis van het authenticatiecookie.
    /// </summary>
    bool IsTeacher();
}
