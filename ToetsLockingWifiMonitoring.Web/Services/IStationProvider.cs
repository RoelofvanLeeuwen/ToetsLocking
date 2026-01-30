namespace ToetsLockingWifiMonitoring.Web.Services
{
    public record Station(string MacAddress);

    public interface IStationProvider
    {
        Task<IReadOnlyCollection<Station>> GetStationsAsync();
    }
}
