using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Components;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Hubs;
using StudentWifiMonitoring.Web.Services;
using StudentWifiMonitoring.Web.Services.Interfaces;

// TeacherAuthService.CookieName en CookieValue zijn internal const en worden gebruikt
// in de login/logout endpoints hieronder.

namespace StudentWifiMonitoring.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddScoped<ITestManagementService, TestManagementService>();
        builder.Services.AddScoped<IStudentRegistrationService, StudentRegistrationService>();
        builder.Services.AddScoped<IStatusService, StatusService>();
        builder.Services.AddScoped<IMyScreenService, MyScreenService>();
        builder.Services.AddScoped<IExportService, ExportService>();
        builder.Services.AddScoped<IDevStationsService, DevStationsService>();
        builder.Services.AddScoped<ITeacherAuthService, TeacherAuthService>();

        builder.Services.AddSignalR();
        builder.Services.AddHostedService<MonitoringService>();
        
        // IStationProvider registratie - RUNTIME bepaald
        if (OperatingSystem.IsWindows())
        {
            // Windows: gebruik MockStationProvider voor development testing
            // Registreer als singleton instance zodat alle consumers dezelfde state delen
            var mockProvider = new MockStationProvider();
            builder.Services.AddSingleton<IStationProvider>(mockProvider);
            builder.Services.AddSingleton(mockProvider); // Ook als concrete type voor DevStations.razor
        }
        else
        {
            // Linux/Raspberry Pi: gebruik echte WiFi station provider
            builder.Services.AddSingleton<IStationProvider, LinuxIwStationProvider>();
        }
        
        // IMacResolver registratie - compile-time bepaald (blijft #if)
#if WINDOWS
        // Registreer de echte resolver
        builder.Services.AddSingleton<WindowsMacResolver>();
        
        // Wrap met development decorator
        builder.Services.AddSingleton<IMacResolver>(sp =>
        {
            var innerResolver = sp.GetRequiredService<WindowsMacResolver>();
            var environment = sp.GetRequiredService<IWebHostEnvironment>();
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<DevelopmentMacResolverDecorator>>();
            return new DevelopmentMacResolverDecorator(innerResolver, environment, configuration, logger);
        });
#else
        // Registreer de echte resolver
        builder.Services.AddSingleton<LinuxMacResolver>();
        
        // Wrap met development decorator
        builder.Services.AddSingleton<IMacResolver>(sp =>
        {
            var innerResolver = sp.GetRequiredService<LinuxMacResolver>();
            var environment = sp.GetRequiredService<IWebHostEnvironment>();
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<DevelopmentMacResolverDecorator>>();
            return new DevelopmentMacResolverDecorator(innerResolver, environment, configuration, logger);
        });
#endif

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode();

        app.MapHub<StatusHub>("/hubs/status");

        // HTTP POST-endpoint voor docent-login.
        // Verwerkt de pincode vóór het verzenden van de response zodat het cookie correct gezet kan worden.
        app.MapPost("/api/teacher/login", (HttpContext httpContext, IConfiguration configuration) =>
        {
            var configuredPassword = configuration["Teacher:Password"] ?? string.Empty;
            var pin = httpContext.Request.Form["pin"].ToString();

            if (!string.IsNullOrEmpty(configuredPassword) && pin == configuredPassword)
            {
                httpContext.Response.Cookies.Append(TeacherAuthService.CookieName, TeacherAuthService.CookieValue, new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Strict,
                    Secure = false // Zet op true als alleen HTTPS gewenst is
                });
                return Results.Redirect("/teacher");
            }

            return Results.Redirect("/teacher?fout=1");
        });

        // HTTP POST-endpoint voor docent-logout.
        app.MapPost("/api/teacher/logout", (HttpContext httpContext) =>
        {
            httpContext.Response.Cookies.Delete(TeacherAuthService.CookieName);
            return Results.Redirect("/teacher");
        });

        app.Run();
    }
}
