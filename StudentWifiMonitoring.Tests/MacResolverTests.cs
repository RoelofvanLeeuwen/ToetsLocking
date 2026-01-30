using StudentWifiMonitoring.Web.Services;

namespace StudentWifiMonitoring.Tests
{
    public class MacResolverTests
    {
        [Fact]
        public void WindowsMacResolver_ReturnsNull_OnInvalidIp()
        {
            var resolver = new WindowsMacResolver();
            var mac = resolver.GetMacForIp("256.256.256.256");
            Assert.Null(mac);
        }

        [Fact]
        public void LinuxMacResolver_ReturnsNull_OnInvalidIp()
        {
            var resolver = new LinuxMacResolver();
            var mac = resolver.GetMacForIp("256.256.256.256");
            Assert.Null(mac);
        }
    }
}
