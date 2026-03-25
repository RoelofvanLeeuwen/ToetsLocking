using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Components;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Hubs;
using StudentWifiMonitoring.Web.Services;

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
        
        builder.Services.AddScoped<DashboardService>();
        
        builder.Services.AddSignalR();
        
#if WINDOWS
                    builder.Services.AddSingleton<IStationProvider, MockStationProvider>();
                    builder.Services.AddSingleton<IMacResolver, WindowsMacResolver>();
#else
        builder.Services.AddSingleton<IStationProvider, LinuxIwStationProvider>();
        builder.Services.AddSingleton<IMacResolver, LinuxMacResolver>();
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

        app.Run();
    }
}
