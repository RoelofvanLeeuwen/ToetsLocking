namespace StudentWifiMonitoring.Web.Services.Wifi;

public record WifiNetwork(string Ssid, int SignalStrength, string SecurityType, bool IsConnected);
