namespace ToetsLockingWifiMonitoring.Web.Domain
{
    public class TestSession
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive => DateTime.UtcNow >= StartTime && DateTime.UtcNow <= EndTime;
    }
}
