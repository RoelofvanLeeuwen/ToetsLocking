namespace StudentWifiMonitoring.Web2.Domain
{
    public class Connection
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student? Student { get; set; }
        public DateTime ConnectedAt { get; set; }
        public DateTime? DisconnectedAt { get; set; }
    }
}
