namespace StudentWifiMonitoring.Web.Services.Wifi;

public enum WifiState { Connected, Disconnected, Connecting }

public record WifiStatus(string? ConnectedSsid, string? IpAddress, WifiState State);
