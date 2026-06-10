using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Domain;
using StudentWifiMonitoring.Web.DTOs.DevStations;
using StudentWifiMonitoring.Web.DTOs.Students;
using StudentWifiMonitoring.Web.Services.Interfaces;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor development-only operaties op de DevStations-pagina.
/// Bevat de database- en mappinglogica voor het ophalen van de actieve toets
/// en het snel aanmaken of bijwerken van teststudenten.
/// </summary>
public class DevStationsService : IDevStationsService
{
    private readonly AppDbContext _db;

    public DevStationsService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<ActiveTestSessionDto?> GetActiveTestSessionAsync()
    {
        var now = DateTime.UtcNow;
        var session = await _db.TestSessions
            .Where(t => t.StartTime <= now && now <= t.EndTime)
            .OrderBy(t => t.StartTime)
            .FirstOrDefaultAsync();

        if (session is null)
            return null;

        return new ActiveTestSessionDto
        {
            Id = session.Id,
            Name = session.Name,
            StartTime = session.StartTime,
            EndTime = session.EndTime
        };
    }

    /// <inheritdoc />
    public async Task<QuickTestResultDto> UpsertTestStudentAsync(string studentName, string macAddress)
    {
        var now = DateTime.UtcNow;
        var activeSession = await _db.TestSessions
            .Where(t => t.StartTime <= now && now <= t.EndTime)
            .OrderBy(t => t.StartTime)
            .FirstOrDefaultAsync();

        if (activeSession is null)
        {
            return new QuickTestResultDto
            {
                Success = false,
                Message = "Geen actieve toets gevonden. Maak eerst een toets aan via /tests."
            };
        }

        var normalizedMac = macAddress.ToLowerInvariant();
        var existingStudent = await _db.Students
            .FirstOrDefaultAsync(s => s.MacAddress == normalizedMac);

        string action;
        if (existingStudent is not null)
        {
            existingStudent.Name = studentName;
            existingStudent.TestName = activeSession.Name;
            action = "bijgewerkt";
        }
        else
        {
            _db.Students.Add(new Student
            {
                Name = studentName,
                MacAddress = normalizedMac,
                TestName = activeSession.Name
            });
            action = "aangemaakt";
        }

        await _db.SaveChangesAsync();

        return new QuickTestResultDto
        {
            Success = true,
            Message = $"Student '{studentName}' {action} voor toets '{activeSession.Name}'."
        };
    }
}
