namespace StudentWifiMonitoring.Web2.Services
{
    public record Station(string MacAddress);

    public interface IStationProvider
    {
        Task<IReadOnlyCollection<Station>> GetStationsAsync();
    }
}
