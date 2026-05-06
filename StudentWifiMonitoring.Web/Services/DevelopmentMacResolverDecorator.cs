namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Decorator voor IMacResolver die een development-only fallback biedt wanneer MAC-adres resolutie mislukt.
/// In Development mode kan deze een mock MAC-adres uit configuratie terugeven als de echte resolver geen resultaat geeft.
/// In productie wordt alleen de echte resolver gebruikt zonder fallback.
/// </summary>
public class DevelopmentMacResolverDecorator : IMacResolver
{
    private readonly IMacResolver _innerResolver;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevelopmentMacResolverDecorator> _logger;

    public DevelopmentMacResolverDecorator(
        IMacResolver innerResolver,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<DevelopmentMacResolverDecorator> logger)
    {
        _innerResolver = innerResolver;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    public string? GetMacForIp(string ipAddress)
    {
        // Probeer eerst de echte resolver
        var macAddress = _innerResolver.GetMacForIp(ipAddress);

        // Als MAC-adres gevonden is, gebruik dat
        if (!string.IsNullOrEmpty(macAddress))
        {
            return macAddress;
        }

        // Development fallback: alleen actief in Development mode
        if (!_environment.IsDevelopment())
        {
            return null;
        }

        // Controleer of mock MAC-adres is ingeschakeld
        var enableMockMac = _configuration.GetValue<bool>("DevelopmentTesting:EnableMockMacAddress", false);
        if (!enableMockMac)
        {
            return null;
        }

        // Haal mock MAC-adres op uit configuratie
        var mockMacAddress = _configuration.GetValue<string>("DevelopmentTesting:MockMacAddress");
        if (string.IsNullOrEmpty(mockMacAddress))
        {
            _logger.LogWarning("DevelopmentTesting:EnableMockMacAddress is true, maar MockMacAddress is niet geconfigureerd");
            return null;
        }

        _logger.LogWarning("Development mode: Gebruik mock MAC-adres {MockMac} voor IP {IpAddress}", 
            mockMacAddress, ipAddress);

        return mockMacAddress;
    }
}