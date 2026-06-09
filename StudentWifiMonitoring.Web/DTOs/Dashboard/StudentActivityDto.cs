using StudentWifiMonitoring.Web.Domain;

namespace StudentWifiMonitoring.Web.DTOs.Dashboard;

public class StudentActivityDto
{
    public DateTime Timestamp { get; set; }
    public EventType EventType { get; set; }
    public string StudentName { get; set; } = string.Empty;
}
