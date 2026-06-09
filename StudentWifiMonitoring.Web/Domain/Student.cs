namespace StudentWifiMonitoring.Web.Domain
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty; // redundante denormalisatie voor simpele filtering
        public string MacAddress { get; set; } = string.Empty;
        public bool IsTestComplete { get; set; }
    }
}
