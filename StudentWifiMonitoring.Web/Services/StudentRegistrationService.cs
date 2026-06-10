using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Domain;
using StudentWifiMonitoring.Web.DTOs.Students;
using StudentWifiMonitoring.Web.Hubs;
using StudentWifiMonitoring.Web.Services.Interfaces;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor het registreren van studenten voor toetssessies.
/// Bevat businesslogica voor het ophalen van de actieve toets, validatie, MAC-resolutie en upsert van studenten.
/// </summary>
public class StudentRegistrationService : IStudentRegistrationService
{
    private readonly AppDbContext _db;
    private readonly IMacResolver _macResolver;
    private readonly IHubContext<StatusHub> _hubContext;
    private readonly ILogger<StudentRegistrationService> _logger;

    public StudentRegistrationService(
        AppDbContext db,
        IMacResolver macResolver,
        IHubContext<StatusHub> hubContext,
        ILogger<StudentRegistrationService> logger)
    {
        _db = db;
        _macResolver = macResolver;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Haalt de huidige actieve toetssessie op.
    /// Een toets is actief als de huidige tijd tussen start- en eindtijd valt.
    /// </summary>
    public async Task<ActiveTestSessionDto?> GetActiveTestSessionAsync()
    {
        var now = DateTime.UtcNow;
        var activeSession = await _db.TestSessions
            .Where(ts => ts.StartTime <= now && ts.EndTime >= now)
            .OrderBy(ts => ts.StartTime)
            .FirstOrDefaultAsync();

        if (activeSession == null)
        {
            return null;
        }

        return new ActiveTestSessionDto
        {
            Id = activeSession.Id,
            Name = activeSession.Name,
            StartTime = activeSession.StartTime,
            EndTime = activeSession.EndTime
        };
    }

    /// <summary>
    /// Registreert een student voor de actieve toetssessie.
    /// Bepaalt automatisch het MAC-adres op basis van het client IP-adres via de MAC resolver.
    /// Voert een upsert uit: nieuwe student wordt toegevoegd, bestaande student (op basis van MAC) wordt bijgewerkt.
    /// </summary>
    public async Task<StudentRegistrationResultDto> RegisterStudentAsync(StudentRegistrationRequestDto request)
    {
        try
        {
            // Valideer naam
            if (string.IsNullOrWhiteSpace(request.StudentName))
            {
                return new StudentRegistrationResultDto
                {
                    Success = false,
                    ErrorMessage = "Voer een geldige naam in."
                };
            }

            // Controleer of er een actieve sessie is
            var activeSessionDto = await GetActiveTestSessionAsync();
            if (activeSessionDto == null)
            {
                return new StudentRegistrationResultDto
                {
                    Success = false,
                    ErrorMessage = "Er is geen actieve toets meer. Probeer de pagina te vernieuwen."
                };
            }

            // Valideer client IP
            if (string.IsNullOrWhiteSpace(request.ClientIpAddress))
            {
                _logger.LogWarning("Client IP-adres is leeg tijdens registratie");
                return new StudentRegistrationResultDto
                {
                    Success = false,
                    ErrorMessage = "Kan je IP-adres niet bepalen. Neem contact op met de docent."
                };
            }

            // Bepaal MAC-adres via resolver (met development fallback in decorator)
            var macAddress = _macResolver.GetMacForIp(request.ClientIpAddress);
            if (string.IsNullOrEmpty(macAddress))
            {
                _logger.LogWarning("Kan MAC-adres niet bepalen voor IP {ClientIp}", request.ClientIpAddress);
                return new StudentRegistrationResultDto
                {
                    Success = false,
                    ErrorMessage = "Kan je apparaat niet identificeren. Zorg dat je verbonden bent met het juiste WiFi-netwerk."
                };
            }

            // Zoek of update bestaande student (upsert)
            var existingStudent = await _db.Students
                .FirstOrDefaultAsync(s => s.MacAddress == macAddress);

            Student student;
            if (existingStudent == null)
            {
                student = new Student
                {
                    Name = request.StudentName.Trim(),
                    TestName = activeSessionDto.Name,
                    MacAddress = macAddress,
                    IsTestComplete = false
                };
                _db.Students.Add(student);
                _logger.LogInformation("Nieuwe student geregistreerd: {StudentName} voor toets {TestName}",
                    student.Name, activeSessionDto.Name);
            }
            else
            {
                existingStudent.Name = request.StudentName.Trim();
                existingStudent.TestName = activeSessionDto.Name;
                existingStudent.IsTestComplete = false;
                student = existingStudent;
                _logger.LogInformation("Bestaande student bijgewerkt: {StudentName} voor toets {TestName}",
                    student.Name, activeSessionDto.Name);
            }

            await _db.SaveChangesAsync();

            // Stuur een SignalR-update als de student al op WiFi zit.
            // Zonder dit blijft het dashboard de oude naam tonen en ziet de student een rood scherm,
            // omdat HandleConnectAsync al eerder vuurde met verouderde gegevens.
            bool hasOpenConnection = await _db.Connections
                .AnyAsync(c => c.StudentId == student.Id && c.DisconnectedAt == null);
            if (hasOpenConnection)
            {
                await _hubContext.Clients.All.SendAsync("status", new
                {
                    mac = macAddress,
                    status = "connected",
                    name = student.Name,
                    testName = activeSessionDto.Name
                });
            }

            return new StudentRegistrationResultDto
            {
                Success = true,
                SuccessMessage = $"Je bent succesvol geregistreerd voor '{activeSessionDto.Name}'. Je apparaat wordt nu gemonitord."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onverwachte fout tijdens registratie van student");
            return new StudentRegistrationResultDto
            {
                Success = false,
                ErrorMessage = "Er is een onverwachte fout opgetreden. Probeer het opnieuw of neem contact op met de docent."
            };
        }
    }
}
