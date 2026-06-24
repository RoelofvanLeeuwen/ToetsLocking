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

    /// <summary>
    /// Valideert de huidige pincode en slaat de nieuwe op in de database.
    /// De database-pincode heeft voorrang boven de geconfigureerde waarde.
    /// Retourneert false als de huidige pincode niet klopt.
    /// </summary>
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);

    /// <summary>Geeft de opgeslagen naam van de docent terug, of null als er nog geen naam ingesteld is.</summary>
    Task<string?> GetNameAsync();

    /// <summary>Slaat de naam van de docent op in de database.</summary>
    Task SaveNameAsync(string name);
}
