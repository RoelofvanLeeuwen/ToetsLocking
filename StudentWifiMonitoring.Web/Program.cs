using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Components;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Hubs;
using StudentWifiMonitoring.Web.Services;
using StudentWifiMonitoring.Web.Services.Interfaces;
using StudentWifiMonitoring.Web.Services.Wifi;

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
builder.Services.AddScoped<IMyScreenService, MyScreenService>();
        builder.Services.AddScoped<StudentNavStateService>();
        builder.Services.AddScoped<IExportService, ExportService>();
        builder.Services.AddScoped<IDevStationsService, DevStationsService>();
        builder.Services.AddScoped<ITeacherAuthService, TeacherAuthService>();
        builder.Services.AddScoped<IAccessPointSettingsService, AccessPointSettingsService>();

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
        
        // IWifiManager registratie - RUNTIME bepaald
        if (OperatingSystem.IsWindows())
            builder.Services.AddSingleton<IWifiManager, MockWifiManager>();
        else
            builder.Services.AddSingleton<IWifiManager, LinuxWifiManager>();

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

        // Voer database migraties automatisch uit bij opstarten.
        // Op een leeg Docker volume wordt de SQLite database en het schema aangemaakt.
        // Op een bestaande database zijn migrations idempotent en doet dit niets.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        // HTTPS redirect is standaard uitgeschakeld zodat de applicatie op de Raspberry Pi
        // via HTTP bereikbaar is zonder certificaatconfiguratie.
        // Zet ForceHttps op true in configuratie (of via Docker environment variable
        // FORCEHTTPS=true) als HTTPS wel vereist is.
        if (app.Configuration.GetValue<bool>("ForceHttps"))
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode();

        app.MapHub<StatusHub>("/hubs/status");

        // Expliciete downloadroute voor het PowerShell-script.
        // UseStaticFiles serveert .ps1 niet standaard (geen MIME-type registratie).
        // Results.File stuurt Content-Disposition: attachment mee zodat de browser altijd downloadt.
        app.MapGet("/download/windows_ics.ps1", (IWebHostEnvironment env) =>
        {
            var path = Path.Combine(env.WebRootPath, "download", "windows_ics.ps1");
            return File.Exists(path)
                ? Results.File(path, "application/octet-stream", "windows_ics.ps1")
                : Results.NotFound();
        });

        // HTTP POST-endpoint voor docent-login.
        // Verwerkt de pincode vóór het verzenden van de response zodat het cookie correct gezet kan worden.
        app.MapPost("/api/teacher/login", async (HttpContext httpContext, IConfiguration configuration, AppDbContext db) =>
        {
            var dbPassword = await db.AppSettings
                .Where(s => s.Key == TeacherAuthService.PasswordSettingKey)
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            var activePassword = dbPassword ?? configuration["Teacher:Password"] ?? string.Empty;
            var pin = httpContext.Request.Form["pin"].ToString();

            if (!string.IsNullOrEmpty(activePassword) && pin == activePassword)
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
