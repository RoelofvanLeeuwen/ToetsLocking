using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Components;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Services;

namespace StudentWifiMonitoring.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
            // Add services to the container.

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddHttpContextAccessor();
#if WINDOWS
                        builder.Services.AddSingleton<IStationProvider, MockStationProvider>();
                        builder.Services.AddSingleton<IMacResolver, WindowsMacResolver>();
#else
            builder.Services.AddSingleton<IStationProvider, LinuxIwStationProvider>();
            builder.Services.AddSingleton<IMacResolver, LinuxMacResolver>();
#endif

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode();



            app.Run();
        }
    }
}
