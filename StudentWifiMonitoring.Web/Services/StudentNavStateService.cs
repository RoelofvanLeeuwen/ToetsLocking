namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Scoped service die de navigatiestatus van de huidige student bijhoudt.
/// Gedeeld tussen NavMenu en MyScreen binnen hetzelfde Blazor-circuit zodat
/// de "Registreren"-link direct reageert op een klik op "Ik ben klaar".
/// </summary>
public class StudentNavStateService
{
    private bool _canRegister = true;

    public bool CanRegister => _canRegister;

    public event Action? OnChange;

    public void SetCanRegister(bool value)
    {
        if (_canRegister == value) return;
        _canRegister = value;
        OnChange?.Invoke();
    }
}
