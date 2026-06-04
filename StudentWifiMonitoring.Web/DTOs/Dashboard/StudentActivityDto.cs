using StudentWifiMonitoring.Web.Domain;

namespace StudentWifiMonitoring.Web.DTOs.Dashboard;

public class StudentActivityDto
{
    public DateTime Timestamp { get; set; }
    public EventType EventType { get; set; }
}
