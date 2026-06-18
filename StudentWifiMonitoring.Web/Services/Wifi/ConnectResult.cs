namespace StudentWifiMonitoring.Web.Services.Wifi;

public enum ConnectOutcome { Success, AuthFailed, NotFound, PermissionDenied, Timeout, OsError }

public record ConnectResult(ConnectOutcome Outcome, string? Message = null);
