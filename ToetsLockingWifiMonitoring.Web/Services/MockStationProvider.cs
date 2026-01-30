namespace ToetsLockingWifiMonitoring.Web.Services
{
    public class MockStationProvider : IStationProvider
    {
        public Task<IReadOnlyCollection<Station>> GetStationsAsync()
        {
            return Task.FromResult<IReadOnlyCollection<Station>>(new List<Station>
            {
                new("aa:bb:cc:dd:ee:01"),
                new("aa:bb:cc:dd:ee:02")
            });
        }
    }
}
