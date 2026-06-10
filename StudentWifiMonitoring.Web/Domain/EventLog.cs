namespace StudentWifiMonitoring.Web.Domain
{
    public class EventLog
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student? Student { get; set; }
        public int TestSessionId { get; set; }
        public TestSession? TestSession { get; set; }
        public EventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public string StudentName { get; set; } = string.Empty;
    }
}
