using Microsoft.AspNetCore.SignalR;

namespace StudentWifiMonitoring.Web.Hubs;

/// <summary>
/// SignalR hub voor real-time communicatie van WiFi-verbindingsstatus.
/// Gebruikt door achtergrondservices om status updates te broadcasten naar alle verbonden clients.
/// </summary>
public class StatusHub : Hub
{
    // Hub heeft geen extra methods nodig; gebruikt alleen voor broadcasting vanuit services
}