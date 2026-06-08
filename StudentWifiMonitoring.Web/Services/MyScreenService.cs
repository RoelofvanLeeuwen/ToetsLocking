using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.DTOs.MyScreen;
using StudentWifiMonitoring.Web.Services.Interfaces;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor het ophalen van de verbindingsstatus van een individuele student.
/// Bevat de querylogica en mapping voor de MyScreen-pagina.
/// </summary>
public class MyScreenService : IMyScreenService
{
    private readonly AppDbContext _db;

    public MyScreenService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Haalt de verbindingsstatus op van de student met het opgegeven MAC-adres.
    /// Geeft de registratiestatus, naam, toetsnaam en actieve verbindingsstatus terug.
    /// </summary>
    public async Task<StudentConnectionStatusDto> GetStudentConnectionStatusAsync(string macAddress)
    {
        var student = await _db.Students
            .FirstOrDefaultAsync(s => s.MacAddress == macAddress);

        if (student == null)
        {
            return new StudentConnectionStatusDto
            {
                IsRegistered = false,
                IsConnected = false
            };
        }

        var now = DateTime.UtcNow;
        var activeSession = await _db.TestSessions
            .FirstOrDefaultAsync(ts => ts.StartTime <= now && ts.EndTime >= now);

        // Alleen verbonden als er een actieve toets is, de student daarvoor geregistreerd is,
        // én een open verbinding heeft. Voorkomt groen scherm door een open verbinding van een
        // verlopen toets waarvan de EndTime stilzwijgend verstreken is.
        bool isConnected = false;
        if (activeSession != null && student.TestName == activeSession.Name)
        {
            isConnected = await _db.Connections
                .AnyAsync(c => c.StudentId == student.Id && c.DisconnectedAt == null);
        }

        return new StudentConnectionStatusDto
        {
            IsRegistered = true,
            IsConnected = isConnected,
            Name = student.Name,
            TestName = student.TestName
        };
    }
}
