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

        var hasOpenConnection = await _db.Connections
            .AnyAsync(c => c.StudentId == student.Id && c.DisconnectedAt == null);

        return new StudentConnectionStatusDto
        {
            IsRegistered = true,
            IsConnected = hasOpenConnection,
            Name = student.Name,
            TestName = student.TestName
        };
    }
}
