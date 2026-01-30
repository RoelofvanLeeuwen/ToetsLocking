using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Domain;
using StudentWifiMonitoring.Web.Services;

namespace StudentWifiMonitoring.Tests
{
    public class MonitoringServiceTests
    {
        [Fact]
        public async Task Monitoring_AddsConnection_WhenActiveTest_AndNewStationAppears()
        {
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("testdb"));
            services.AddLogging();

            var providerMock = new Mock<IStationProvider>();
            providerMock.Setup(p => p.GetStationsAsync()).ReturnsAsync(new[] { new Station("aa:bb:cc:dd:ee:01") });
            services.AddSingleton(providerMock.Object);

            var sp = services.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            db.Students.Add(new Student { Name = "Test", TestName = "Demo", MacAddress = "aa:bb:cc:dd:ee:01" });
            db.TestSessions.Add(new TestSession { Name = "Demo", StartTime = DateTime.UtcNow.AddMinutes(-10), EndTime = DateTime.UtcNow.AddMinutes(10) });
            db.SaveChanges();

            var logger = sp.GetRequiredService<ILogger<MonitoringService>>();
            var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "Monitoring:PollSeconds", "1" } }).Build();
            var svc = new MonitoringService(logger, sp, providerMock.Object, cfg);

            var cts = new CancellationTokenSource();
            var task = svc.StartAsync(cts.Token);
            await Task.Delay(1200);
            cts.Cancel();

            Assert.Single(db.Connections.ToList());
        }

        [Fact]
        public async Task Monitoring_DoesNothing_WhenNoActiveTest()
        {
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("testdb2"));
            services.AddLogging();

            var providerMock = new Mock<IStationProvider>();
            providerMock.Setup(p => p.GetStationsAsync()).ReturnsAsync(new[] { new Station("aa:bb:cc:dd:ee:01") });
            services.AddSingleton(providerMock.Object);

            var sp = services.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            db.Students.Add(new Student { Name = "Test", TestName = "Demo", MacAddress = "aa:bb:cc:dd:ee:01" });
            db.SaveChanges();

            var logger = sp.GetRequiredService<ILogger<MonitoringService>>();
            var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "Monitoring:PollSeconds", "1" } }).Build();
            var svc = new MonitoringService(logger, sp, providerMock.Object, cfg);

            var cts = new CancellationTokenSource();
            var task = svc.StartAsync(cts.Token);
            await Task.Delay(1200);
            cts.Cancel();

            Assert.Empty(db.Connections.ToList());
            Assert.Empty(db.Events.ToList());
        }
    }
}
