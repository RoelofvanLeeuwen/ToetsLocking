using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Domain;
using StudentWifiMonitoring.Web.DTOs.Tests;
using StudentWifiMonitoring.Web.Hubs;
using StudentWifiMonitoring.Web.Services.Interfaces;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor het beheren van toetssessies.
/// Bevat businesslogica voor CRUD-operaties, filtering, paginering en statusbeheer van toetsen.
/// </summary>
public class TestManagementService : ITestManagementService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<StatusHub> _hubContext;
    private readonly ILogger<TestManagementService> _logger;

    public TestManagementService(
        AppDbContext db, 
        IHubContext<StatusHub> hubContext, 
        ILogger<TestManagementService> logger)
    {
        _db = db;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Haalt een gefilterde en gepagineerde lijst van toetssessies op.
    /// Actieve toetsen worden bovenaan getoond, overige van nieuw naar oud.
    /// </summary>
    public async Task<PagedResultDto<TestSessionDto>> GetPagedTestsAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var now = DateTime.UtcNow; // Voor SQL-vertaalbare vergelijking
        var query = _db.TestSessions.AsQueryable();

        // Filter op zoekterm (case-insensitive)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(searchLower));
        }

        // Tel totaal aantal resultaten
        var totalCount = await query.CountAsync();

        // Sorteer: actieve toetsen bovenaan (SQL-vertaalbare expressie), dan nieuw naar oud
        var entities = await query
            .OrderByDescending(t => t.StartTime <= now && t.EndTime >= now) // IsActive logica, SQL-vertaalbaar
            .ThenByDescending(t => t.StartTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Map entities naar DTO's
        var items = entities.Select(MapToDto).ToList();

        return new PagedResultDto<TestSessionDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Maakt een nieuwe toetssessie aan.
    /// Valideert de input en converteert lokale tijden naar UTC.
    /// </summary>
    public async Task<OperationResultDto> CreateTestAsync(TestSessionCreateDto createDto)
    {
        try
        {
            // Validatie
            if (string.IsNullOrWhiteSpace(createDto.Name))
            {
                return new OperationResultDto
                {
                    Success = false,
                    ErrorMessage = "Voer een toetsnaam in."
                };
            }

            if (createDto.EndTimeLocal <= createDto.StartTimeLocal)
            {
                return new OperationResultDto
                {
                    Success = false,
                    ErrorMessage = "Eindtijd moet na de starttijd liggen."
                };
            }

            // Converteer lokale tijd naar UTC
            var startUtc = DateTime.SpecifyKind(createDto.StartTimeLocal, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(createDto.EndTimeLocal, DateTimeKind.Local).ToUniversalTime();

            // Maak entity aan
            var session = new TestSession
            {
                Name = createDto.Name.Trim(),
                StartTime = startUtc,
                EndTime = endUtc
            };

            _db.TestSessions.Add(session);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Toets aangemaakt: {TestName} ({StartTime} - {EndTime})",
                session.Name, session.StartTime, session.EndTime);

            return new OperationResultDto { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij het aanmaken van toets");
            return new OperationResultDto
            {
                Success = false,
                ErrorMessage = "Er is een fout opgetreden bij het aanmaken van de toets."
            };
        }
    }

    /// <summary>
    /// Werkt een bestaande toetssessie bij.
    /// Valideert de input en converteert lokale tijden naar UTC.
    /// </summary>
    public async Task<OperationResultDto> UpdateTestAsync(TestSessionUpdateDto updateDto)
    {
        try
        {
            // Validatie
            if (string.IsNullOrWhiteSpace(updateDto.Name))
            {
                return new OperationResultDto
                {
                    Success = false,
                    ErrorMessage = "Voer een toetsnaam in."
                };
            }

            if (updateDto.EndTimeLocal <= updateDto.StartTimeLocal)
            {
                return new OperationResultDto
                {
                    Success = false,
                    ErrorMessage = "Eindtijd moet na de starttijd liggen."
                };
            }

            // Haal toets op
            var test = await _db.TestSessions.FindAsync(updateDto.Id);
            if (test == null)
            {
                return new OperationResultDto
                {
                    Success = false,
                    ErrorMessage = "Toets niet gevonden."
                };
            }

            // Converteer lokale tijd naar UTC
            var startUtc = DateTime.SpecifyKind(updateDto.StartTimeLocal, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(updateDto.EndTimeLocal, DateTimeKind.Local).ToUniversalTime();

            // Update entity
            test.Name = updateDto.Name.Trim();
            test.StartTime = startUtc;
            test.EndTime = endUtc;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Toets bijgewerkt: {TestName} ({StartTime} - {EndTime})",
                test.Name, test.StartTime, test.EndTime);

            return new OperationResultDto { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij het bijwerken van toets");
            return new OperationResultDto
            {
                Success = false,
                ErrorMessage = "Er is een fout opgetreden bij het opslaan van de toets."
            };
        }
    }

    /// <summary>
    /// Zet een actieve toetssessie inactief door de eindtijd te wijzigen.
    /// Sluit automatisch alle open verbindingen van studenten die voor deze toets geregistreerd zijn.
    /// </summary>
    public async Task<TestDeactivationResultDto> DeactivateTestAsync(int testId)
    {
        var test = await _db.TestSessions.FindAsync(testId);
        if (test == null)
        {
            throw new InvalidOperationException("Toets niet gevonden.");
        }

        var now = DateTime.UtcNow;
        var disconnectCount = 0;

        // Zet toets inactief
        test.EndTime = now.AddSeconds(-1);

        // Haal studenten op die voor deze toets zijn geregistreerd
        var students = await _db.Students
            .Where(s => s.TestName == test.Name)
            .ToListAsync();

        if (students.Count > 0)
        {
            var studentIds = students.Select(s => s.Id).ToList();

            // Sluit alle open verbindingen
            var openConnections = await _db.Connections
                .Where(c => studentIds.Contains(c.StudentId) && c.DisconnectedAt == null)
                .ToListAsync();

            foreach (var connection in openConnections)
            {
                connection.DisconnectedAt = now;
                disconnectCount++;
            }

            // Registreer events en stuur SignalR notificaties
            foreach (var student in students)
            {
                var hadOpenConnection = openConnections.Any(c => c.StudentId == student.Id);
                if (hadOpenConnection)
                {
                    // Registreer disconnect event
                    var eventLog = new EventLog
                    {
                        StudentId = student.Id,
                        TestSessionId = test.Id,
                        EventType = EventType.Disconnected,
                        Timestamp = now
                    };
                    _db.Events.Add(eventLog);

                    // Stuur SignalR notificatie
                    await _hubContext.Clients.All.SendAsync("status", new
                    {
                        mac = student.MacAddress,
                        status = "disconnected",
                        name = student.Name,
                        testName = student.TestName
                    });

                    _logger.LogInformation(
                        "Student {Name} ({MacAddress}) automatisch verbroken door toets inactief zetten: {TestName}",
                        student.Name, student.MacAddress, test.Name);
                }
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Toets inactief gezet: {TestName}, {DisconnectCount} verbindingen gesloten",
            test.Name, disconnectCount);

        return new TestDeactivationResultDto
        {
            TestName = test.Name,
            DisconnectedStudentsCount = disconnectCount
        };
    }

    /// <summary>
    /// Map een TestSession entity naar een TestSessionDto.
    /// </summary>
    private TestSessionDto MapToDto(TestSession entity)
    {
        var now = DateTime.UtcNow;
        return new TestSessionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            IsActive = entity.StartTime <= now && entity.EndTime >= now
        };
    }
}