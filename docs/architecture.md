# StudentWifiMonitoring - Technische Architectuur

## 1. Projectbeschrijving

StudentWifiMonitoring is een .NET 8 applicatie die draait op een Raspberry Pi en lokaal op Windows ontwikkeld kan worden. De applicatie monitort welke studenten via WiFi verbonden zijn tijdens een actieve toets en registreert connect/disconnect events.

**Kernfunctionaliteit:**
- Studenten registreren zich voor een toets via hun apparaat
- Het systeem detecteert het MAC-adres van het apparaat via het IP-adres
- Tijdens een actieve toets monitort een achtergrondservice welke apparaten online/offline gaan
- Een dashboard toont real-time welke studenten verbonden zijn
- Alle events worden opgeslagen in een SQLite database

**Doelgroep:**
- Docenten die toetsen beheren
- Studenten die zich registreren voor toetsen
- Beheerders die monitoring data bekijken

---

## 2. Technologiestack

### Framework & Runtime
- **.NET 8.0** - Modern cross-platform framework
- **C# 12** - Primaire programmeertaal
- **Blazor Server** - Interactive server-side UI framework
- **ASP.NET Core** - Web hosting

### Database & ORM
- **Entity Framework Core** - ORM voor database toegang
- **SQLite** - Lichtgewicht embedded database
- **EF Core Migrations** - Database schema versioning

### Real-time Communicatie
- **SignalR** - WebSocket-based real-time updates naar clients

### Infrastructuur
- **Raspberry Pi OS (Linux)** - Productie deployment
- **Windows 10/11** - Lokale ontwikkeling
- **systemd** - Service management op Linux (niet geïmplementeerd in deze versie)

### Dependencies
- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.AspNetCore.SignalR`
- Platform-specifieke shell commando's (`ip neigh` op Linux, `arp` op Windows)

---

## 3. Projectstructuur

```
StudentWifiMonitoring.Web/
├── Components/
│   ├── Pages/              # Blazor pagina's
│   │   ├── Home.razor
│   │   ├── Register.razor
│   │   ├── Tests.razor
│   │   └── Dashboard.razor
│   ├── Layout/             # Layout componenten
│   └── App.razor
├── Data/
│   └── AppDbContext.cs     # EF Core DbContext
├── Domain/
│   ├── Student.cs          # Domain entities
│   ├── TestSession.cs
│   ├── Connection.cs
│   ├── EventLog.cs
│   └── EventType.cs
├── Services/
│   ├── DashboardService.cs # Business logica voor dashboard
│   ├── MonitoringService.cs # Background monitoring
│   ├── IStationProvider.cs # WiFi station detection
│   ├── MacResolver.cs      # MAC-adres resolutie
│   └── DevelopmentMacResolverDecorator.cs
├── Hubs/
│   └── StatusHub.cs        # SignalR hub
├── Migrations/             # EF Core migrations
├── wwwroot/                # Static files
├── appsettings.json        # Productie configuratie
├── appsettings.Development.json # Development configuratie
└── Program.cs              # Application entry point
```

---

## 4. Belangrijkste Onderdelen

### 4.1 Blazor Pagina's

**Verantwoordelijkheid:** UI rendering en user interaction handling

**Architectuur:** Blazor pagina's werken via service-interfaces en DTO's. Directe toegang tot `AppDbContext` is niet toegestaan.

#### Register.razor
- Student registratiepagina
- Vraagt naam van student
- Bepaalt automatisch MAC-adres via `IMacResolver`
- **Gebruik:** `IStudentService` voor registratie, werkt met DTO's
- Gebruikt `@rendermode InteractiveServer` voor Blazor Server interactiviteit

**Belangrijke implementatiedetails:**
- Valideert naam server-side (geen lege strings)
- Controleert of er een actieve toets is via service
- Gebruikt `IHttpContextAccessor` voor client IP-adres
- Logt alle belangrijke acties via `ILogger`

#### Tests.razor
- Beheer van toetssessies
- Toetsen aanmaken met starttijd en eindtijd
- Lokale tijd invoer via `datetime-local` input
- Converteert lokale tijd naar UTC voor opslag
- **Gebruik:** `ITestSessionService` voor CRUD operaties, werkt met `TestSessionDto`

**Belangrijke implementatiedetails:**
- Valideert dat eindtijd na starttijd ligt
- Gebruikt `DateTime.SpecifyKind()` voor expliciete tijdzone conversie
- Toont tijden in lokale tijdzone via `.ToLocalTime()`

#### Dashboard.razor
- Overzicht van geregistreerde studenten
- Real-time online/offline status
- Filtering op toetssessie
- SignalR client voor live updates
- **Gebruik:** `IDashboardService` voor businesslogica, werkt met DTO's

**Belangrijke implementatiedetails:**
- Gebruikt `IDashboardService` voor data ophalen
- Luistert naar SignalR "status" events
- Herlaadt data wanneer student online/offline gaat

#### Home.razor
- Landingspagina met uitleg over de applicatie

---

### 4.2 Services

**Verantwoordelijkheid:** Business logica en infrastructuur concerns gescheiden van UI

#### DashboardService
**Type:** Scoped service  
**Verantwoordelijkheid:** Business logica voor dashboard data

```csharp
public class DashboardService
{
    public List<TestSession> GetTestSessions()
    public (List<Student> Students, HashSet<string> OnlineMacs) GetStudentsWithStatus(int? testSessionId)
}
```

**Belangrijke logica:**
- Haalt studenten en online status op uit database
- Filtert op toetssessie indien opgegeven
- Determineert online status via open `Connection` entities

#### MonitoringService
**Type:** BackgroundService (Hosted Service)  
**Verantwoordelijkheid:** Periodieke monitoring van WiFi-verbindingen

**Runtime gedrag:**
1. Draait elke N seconden (configureerbaar via `Monitoring:PollSeconds`)
2. Controleert of er een actieve `TestSession` is
3. Haalt verbonden MAC-adressen op via `IStationProvider`
4. Vergelijkt met vorige poll om nieuwe/verbroken verbindingen te detecteren
5. Registreert `Connection` en `EventLog` entities
6. Stuurt SignalR events naar alle verbonden clients

**Belangrijke implementatiedetails:**
- Gebruikt in-memory `HashSet<string>` voor laatst bekende online MACs
- Gebruikt scoped `AppDbContext` via `IServiceProvider`
- Reset state wanneer geen actieve toets is
- Logt warnings voor onbekende MAC-adressen

#### IStationProvider
**Verantwoordelijkheid:** Platform-specifieke WiFi station detection

**Implementaties:**
- `LinuxIwStationProvider` - Gebruikt `iw dev wlan0 station dump`
- `MockStationProvider` - Windows development mock

**Conditional compilation:**
```csharp
#if WINDOWS
    builder.Services.AddSingleton<IStationProvider, MockStationProvider>();
#else
    builder.Services.AddSingleton<IStationProvider, LinuxIwStationProvider>();
#endif
```

#### IMacResolver
**Verantwoordelijkheid:** IP-adres naar MAC-adres resolutie

**Implementaties:**
- `LinuxMacResolver` - Gebruikt `ip neigh show`
- `WindowsMacResolver` - Gebruikt `arp -a`
- `DevelopmentMacResolverDecorator` - Development fallback wrapper

**Decorator pattern:**
```
IMacResolver (interface)
    ├── LinuxMacResolver (platform-specifiek)
    ├── WindowsMacResolver (platform-specifiek)
    └── DevelopmentMacResolverDecorator (wrapper)
            └── Wraps LinuxMacResolver or WindowsMacResolver
```

**Resolutieflow:**
1. Decorator roept inner resolver aan (`LinuxMacResolver` of `WindowsMacResolver`)
2. Als MAC gevonden → return direct
3. Als MAC niet gevonden:
   - In productie → return `null`
   - In development (en flag enabled) → return mock MAC uit configuratie

---

### 4.3 AppDbContext

**Verantwoordelijkheid:** EF Core database context met UTC ValueConverter voor tijdzones

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Student> Students { get; }
    public DbSet<Connection> Connections { get; }
    public DbSet<EventLog> Events { get; }
    public DbSet<TestSession> TestSessions { get; }
}
```

**Belangrijke configuratie in `OnModelCreating`:**

#### UTC ValueConverter voor TestSession
```csharp
var utcConverter = new ValueConverter<DateTime, DateTime>(
    v => v.ToUniversalTime(),              // Bij opslaan: converteer naar UTC
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)); // Bij uitlezen: markeer als UTC

b.Entity<TestSession>()
    .Property(e => e.StartTime)
    .HasConversion(utcConverter);
```

**Waarom dit nodig is:**
- SQLite slaat DateTime op als TEXT in ISO 8601 formaat
- Zonder expliciete `DateTimeKind` weet .NET niet of een waarde UTC of Local is
- De converter garandeert dat alle opgeslagen waarden UTC zijn
- Bij uitlezen wordt de `DateTimeKind.Utc` flag gezet zodat queries met `DateTime.UtcNow` correct werken

**Relaties:**
- `Student` 1-N `Connection`
- `Student` 1-N `EventLog`
- `TestSession` 1-N `EventLog`

**Indexes:**
- `Student.MacAddress` - Unique index voor snelle lookups

---

### 4.4 Domain Models

#### Student
```csharp
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string TestName { get; set; }  // Denormalized voor filtering
    public string MacAddress { get; set; } // Unique identifier
}
```

#### TestSession
```csharp
public class TestSession
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; set; }  // UTC via ValueConverter
    public DateTime EndTime { get; set; }    // UTC via ValueConverter
    public bool IsActive => DateTime.UtcNow >= StartTime && DateTime.UtcNow <= EndTime;
}
```

**Belangrijk:** De computed property `IsActive` gebruikt `DateTime.UtcNow` wat correct werkt omdat de ValueConverter garandeert dat `StartTime` en `EndTime` UTC zijn.

#### Connection
```csharp
public class Connection
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
}
```

**Gebruik:** Een open connection (waar `DisconnectedAt == null`) betekent dat de student momenteel online is.

#### EventLog
```csharp
public class EventLog
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int TestSessionId { get; set; }
    public EventType EventType { get; set; }  // Connected / Disconnected
    public DateTime Timestamp { get; set; }
}
```

**Gebruik:** Auditlog van alle connect/disconnect events tijdens toetsen.

---

### 4.5 SignalR Hub

```csharp
public class StatusHub : Hub
{
    // Geen custom methods nodig; gebruikt voor broadcasting vanuit services
}
```

**Gebruik:**
- MonitoringService stuurt via `IHubContext<StatusHub>` berichten naar alle clients
- Clients (Dashboard) luisteren naar deze berichten via JavaScript SignalR client

**Message format:**
```json
{
    "mac": "aa:bb:cc:dd:ee:ff",
    "status": "connected",
    "name": "Student Naam",
    "testName": "Toetsnaam"
}
```

---

## 5. Verantwoordelijkheden per Laag

### UI Laag (Blazor Components)
**Verantwoordelijkheid:**
- User input verwerken
- Data presenteren
- Simpele UI-state management
- Services aanroepen voor businesslogica
- Simpele validatie (null checks, required fields)

**Mag NIET:**
- Complexe businesslogica bevatten
- **Direct `AppDbContext` of EF Core entities gebruiken**
- Platform-specifieke code bevatten
- Configuratie-logica bevatten

**Gebruik:**
- Services via interfaces (bijv. `IStudentService`, `ITestSessionService`)
- DTO's voor data transport tussen UI en services

**Voorbeeld goed (huidige implementatie):**
```csharp
// Register.razor
var macAddress = MacResolver.GetMacForIp(clientIp);
if (string.IsNullOrEmpty(macAddress))
{
    _errorMessage = "Kan je apparaat niet identificeren.";
    return;
}
```

**Voorbeeld fout:**
```csharp
// NIET DOEN in Register.razor
if (Environment.IsDevelopment() && Configuration["EnableMock"] == "true")
{
    macAddress = Configuration["MockMac"];
}
```

### Service Laag
**Verantwoordelijkheid:**
- Businesslogica implementeren
- Data transformaties
- **Mapping tussen domain entities en DTO's**
- Database queries via EF Core (in deze fase direct via `AppDbContext`)
- Externe systemen aanroepen (shell commands)
- Configuratie-logica

**Gebruik:**
- Services worden gedefinieerd via interfaces
- Services gebruiken `AppDbContext` direct via constructor injection
- Services retourneren DTO's naar de UI, niet entities

**Voorbeelden:**
- `DashboardService` - Dashboard-specifieke queries en filtering
- `MonitoringService` - Monitoring loop en event detectie
- `DevelopmentMacResolverDecorator` - Development-specifieke infrastructuur logica

**Gewenste structuur (voor refactoring):**
- `IStudentService` + `StudentService` - Student-gerelateerde businesslogica
- `ITestSessionService` + `TestSessionService` - Toetssessie-gerelateerde businesslogica

### Data Laag (AppDbContext + Domain)
**Verantwoordelijkheid:**
- Database schema definitie
- Relaties configureren
- Type conversies (ValueConverters)
- Constraints en indexes

**Gebruik:**
- **`AppDbContext` wordt alleen gebruikt door services, niet door Blazor pagina's**
- Entities worden alleen binnen de service-laag gebruikt

**Mag NIET:**
- Businesslogica bevatten
- Services aanroepen

---

## 6. Gewenste Doelstructuur voor Komende Refactorstappen

### Huidige Tussenfase: Services met Interfaces en DTO's

De applicatie gebruikt een duidelijke scheiding tussen UI, services en data:

1. **Blazor pagina's gebruiken nooit direct `AppDbContext`**
2. **Services met interfaces bevatten alle businesslogica**
3. **DTO's worden gebruikt voor communicatie tussen UI en services**
4. **Services gebruiken voorlopig direct EF Core en `AppDbContext`**

### Mogelijke Vervolgstap: Repository Pattern

Op termijn kan een repository laag toegevoegd worden tussen services en `AppDbContext`:
- Repositories abstraheren data toegang
- Services gebruiken dan repositories in plaats van direct `AppDbContext`
- Dit maakt testen en database-onafhankelijkheid eenvoudiger

**Belangrijk:** Deze stap maken we **nu nog niet**. De huidige tussenfase met services is voldoende en pragmatisch.

#### Lagen in de huidige tussenstructuur

```
┌─────────────────────────────────────┐
│   Blazor Pages (UI)                 │
│   - Register.razor                  │
│   - Tests.razor                     │
│   - Dashboard.razor                 │
│   Gebruikt: Service interfaces      │
│   Werkt met: DTO's                  │
└─────────────────────────────────────┘
              ↓ ↑ DTO's
┌─────────────────────────────────────┐
│   Services + Interfaces             │
│   - IStudentService                 │
│   - ITestSessionService             │
│   - StudentService                  │
│   - TestSessionService              │
│   Bevat: Businesslogica, mapping    │
│   Gebruikt: AppDbContext (direct)   │
└─────────────────────────────────────┘
              ↓ ↑ Entities
┌─────────────────────────────────────┐
│   Data + Domain                     │
│   - AppDbContext                    │
│   - Domain entities                 │
└─────────────────────────────────────┘
```

**Mogelijke vervolgstap (nu nog niet):**
Een repository laag kan later toegevoegd worden tussen Services en Data, waardoor services repositories gebruiken in plaats van direct `AppDbContext`.

#### Voorbeeld: Register Flow

**Implementatie met services en DTO's:**
```csharp
// Register.razor - gebruikt service interface
@inject IStudentService StudentService

private async Task RegisterAsync()
{
    var result = await StudentService.RegisterStudentAsync(studentName, macAddress, testSessionId);
    if (result.Success)
    {
        // Success handling
    }
    else
    {
        _errorMessage = result.ErrorMessage;
    }
}

// StudentService.cs - bevat businesslogica
public class StudentService : IStudentService
{
    private readonly AppDbContext _context;

    public async Task<RegistrationResultDto> RegisterStudentAsync(string name, string macAddress, int testSessionId)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(name))
            return new RegistrationResultDto { Success = false, ErrorMessage = "Naam is verplicht" };

        // Check duplicate
        var exists = await _context.Students.AnyAsync(s => s.MacAddress == macAddress);
        if (exists)
            return new RegistrationResultDto { Success = false, ErrorMessage = "Apparaat is al geregistreerd" };

        // Create entity
        var student = new Student { Name = name, MacAddress = macAddress, TestSessionId = testSessionId };
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        // Map naar DTO
        return new RegistrationResultDto 
        { 
            Success = true, 
            StudentId = student.Id,
            Student = new StudentDto 
            { 
                Id = student.Id, 
                Name = student.Name, 
                MacAddress = student.MacAddress 
            }
        };
    }
}
```

#### DTOs (Data Transfer Objects)

DTO's zijn simpele klassen voor data transport tussen UI en services:

```csharp
// DTOs/StudentDto.cs
public class StudentDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string MacAddress { get; set; }
    public bool IsOnline { get; set; }  // Computed in service
}

// DTOs/TestSessionDto.cs
public class TestSessionDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; }  // Computed in service
}

// DTOs/RegistrationResultDto.cs
public class RegistrationResultDto
{
    public bool Success { get; set; }
    public int? StudentId { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### Implementatie Aanpak

**Stapsgewijze implementatie per functioneel gebied:**

1. **Student registratie**
   - Implementeer `IStudentService` + `StudentService`
   - Maak benodigde DTO's (`StudentDto`, `RegistrationResultDto`)
   - Implementeer `Register.razor` met service

2. **Toetssessie beheer**
   - Implementeer `ITestSessionService` + `TestSessionService`
   - Maak benodigde DTO's (`TestSessionDto`)
   - Implementeer `Tests.razor` met service

3. **Dashboard**
   - Implementeer `IDashboardService` interface voor bestaande `DashboardService`
   - Introduceer DTO's voor dashboard data
   - Update `Dashboard.razor` om met DTO's te werken

4. **Validatie en testen**
   - Test alle functionaliteit na elke stap
   - Verifieer dat gedrag correct werkt

### Wat NIET in de huidige implementatie

- **Geen repository pattern** - Services gebruiken voorlopig direct `AppDbContext`. Repositories zijn een mogelijke vervolgstap.
- **Geen nieuwe projectlagen** - Gebruik bestaande mappen (`Services`, `DTOs`, `Domain`, `Data`)
- **Geen wijzigingen aan werkende background services** - `MonitoringService` blijft zoals het is

### Voordelen van deze structuur

1. **Testbaarheid** - Services zijn makkelijk te unit testen
2. **Herbruikbaarheid** - Businesslogica kan hergebruikt worden door andere consumers (API, background jobs)
3. **Scheiding van verantwoordelijkheden** - UI doet UI, services doen businesslogica
4. **Onderhoudbaarheid** - Logica is geïsoleerd en makkelijker te wijzigen
5. **Toekomstbestendig** - Eenvoudig uit te breiden met repository pattern als dat later nodig is

---

## 7. Waarom Businesslogica niet in Blazor Componenten Hoort

### Redenen

#### 1. Testbaarheid
**Probleem:** Blazor componenten zijn moeilijk te unit testen
```csharp
// Moeilijk te testen: logica in component
@code {
    private async Task RegisterAsync()
    {
        // 50 regels complexe registratielogica
    }
}
```

**Oplossing:** Service is eenvoudig te unit testen
```csharp
// Makkelijk te testen: logica in service
public class RegistrationService
{
    public async Task<RegistrationResult> RegisterStudentAsync(...)
    {
        // Logica hier
    }
}

// Test
[Fact]
public async Task RegisterStudent_WithValidData_Succeeds()
{
    var service = new RegistrationService(mockDb, mockLogger);
    var result = await service.RegisterStudentAsync(...);
    Assert.True(result.Success);
}
```

#### 2. Herbruikbaarheid
Als registratielogica in `Register.razor` staat, kan het niet hergebruikt worden door:
- Een API endpoint
- Een andere pagina
- Een console command
- Een background job

Met een service is hergebruik triviaal:
```csharp
// In Blazor component
registrationService.RegisterStudentAsync(...)

// In API controller
registrationService.RegisterStudentAsync(...)

// In background job
registrationService.RegisterStudentAsync(...)
```

#### 3. Separation of Concerns
**UI verantwoordelijkheid:**
- Weergeven van data
- User input verzamelen
- Navigatie

**Business verantwoordelijkheid:**
- Validatieregels
- Domeinlogica
- Database transacties

Door deze te mixen wordt code:
- Moeilijk te begrijpen
- Moeilijk te onderhouden
- Moeilijk te wijzigen

#### 4. Code Reuse in dit Project
In dit project gebruikt zowel `Register.razor` als `MonitoringService` de volgende services:
- `AppDbContext` - voor database toegang
- `IMacResolver` - voor MAC-adres resolutie

Als MAC-resolutie logica in `Register.razor` zou zitten, moest het gedupliceerd worden in `MonitoringService`.

---

## 8. Waarom Development Fallback in Lager Architectuurniveau Staat

### Probleem
Op localhost (::1 of 127.0.0.1) staat het client MAC-adres niet in de ARP/neighbor table. Dit maakt lokale ontwikkeling onmogelijk zonder infrastructuur-aanpassingen.

### Waarom NIET in Register.razor?

#### 1. Violation of Single Responsibility
```csharp
// VERKEERD: Register.razor met development logica
@code {
    private async Task RegisterAsync()
    {
        var mac = MacResolver.GetMacForIp(ip);
        
        // Development fallback logica in UI component
        if (string.IsNullOrEmpty(mac) && Environment.IsDevelopment())
        {
            if (Configuration["EnableMock"] == "true")
                mac = Configuration["MockMac"];
        }
    }
}
```

**Problemen:**
- UI component heeft ineens infrastructuur kennis
- Environment check in UI code
- Configuration toegang in UI code
- Als andere componenten ook MAC-resolutie nodig hebben, moet dezelfde logica gedupliceerd worden

#### 2. Geen Separation of Concerns
Development infrastructure concerns horen niet in de UI laag. De UI zou niet moeten weten:
- Of de app in Development of Production draait
- Hoe MAC-adressen opgelost worden
- Wat een "fallback" betekent

### Waarom WEL in DevelopmentMacResolverDecorator?

#### 1. Decorator Pattern
```
IMacResolver
    ↓
DevelopmentMacResolverDecorator (development concern)
    ↓
WindowsMacResolver (platform concern)
```

**Voordelen:**
- Transparant voor callers
- `Register.razor` weet niet van development mode
- Andere componenten krijgen dezelfde fallback gratis

#### 2. Infrastructure Concern
MAC-resolutie is infrastructuur, geen UI:
- Platform-afhankelijk (Linux vs Windows)
- Environment-afhankelijk (Development vs Production)
- Configuratie-afhankelijk

**Infrastructuur hoort in de infrastructuur laag, niet in UI.**

#### 3. Open/Closed Principle
Nieuwe callers van `IMacResolver` krijgen automatisch de development fallback:
```csharp
// Nieuwe component
@inject IMacResolver MacResolver

@code {
    // Werkt automatisch met of zonder development fallback
    var mac = MacResolver.GetMacForIp(ip);
}
```

Geen code duplication, geen development checks in UI.

#### 4. Single Point of Configuration
Development gedrag wordt geconfigureerd op één plek:
```json
// appsettings.Development.json
{
  "DevelopmentTesting": {
    "EnableMockMacAddress": true,
    "MockMacAddress": "aa:bb:cc:dd:ee:ff"
  }
}
```

Als dit in `Register.razor` zou staan, zou elke component die MAC-resolutie nodig heeft dezelfde configuratie moeten lezen.

---

## 8. Belangrijkste Runtime Flows

### 8.1 Student Registratie Flow

```
1. Student navigeert naar /register
   ↓
2. Register.razor.OnInitializedAsync()
   ↓ Query naar DbContext
3. Zoek actieve TestSession (StartTime <= UtcNow <= EndTime)
   ↓
4. Toon registratieformulier met toetsnaam
   ↓
5. Student vult naam in en klikt "Registreren"
   ↓
6. Register.razor.RegisterAsync()
   ↓
7. Valideer naam (niet leeg)
   ↓
8. Haal client IP op via HttpContextAccessor
   ↓
9. MacResolver.GetMacForIp(clientIp)
   ↓ DevelopmentMacResolverDecorator
10. Probeer WindowsMacResolver (arp -a)
   ↓
11. Als geen MAC en Development: gebruik mock MAC
   ↓
12. Als geen MAC: toon foutmelding en stop
   ↓
13. Zoek Student met MacAddress in database
   ↓
14a. Niet gevonden: maak nieuwe Student aan
14b. Wel gevonden: update Name en TestName
   ↓
15. SaveChangesAsync()
   ↓
16. Toon succesmelding
```

**Database transactie:**
```sql
-- Nieuwe student
INSERT INTO Students (Name, TestName, MacAddress) 
VALUES ('Jan Jansen', 'Wiskunde Toets', 'aa:bb:cc:dd:ee:ff');

-- Of bestaande student update
UPDATE Students 
SET Name = 'Jan Jansen', TestName = 'Wiskunde Toets'
WHERE MacAddress = 'aa:bb:cc:dd:ee:ff';
```

---

### 8.2 Actieve Toets Detectie Flow

**Gebruikt door:** `Register.razor`, `MonitoringService`

```
1. Bepaal huidige tijd: DateTime.UtcNow
   ↓
2. Query naar database:
   SELECT * FROM TestSessions
   WHERE StartTime <= @now AND EndTime >= @now
   ORDER BY StartTime
   LIMIT 1
   ↓
3a. TestSession gevonden: gebruik deze
3b. Geen TestSession: toon "geen actieve toets" of stop monitoring
```

**EF Core query:**
```csharp
var now = DateTime.UtcNow;
var activeSession = await context.TestSessions
    .Where(ts => ts.StartTime <= now && ts.EndTime >= now)
    .OrderBy(ts => ts.StartTime)
    .FirstOrDefaultAsync();
```

**Waarom UTC:**
- ValueConverter in AppDbContext garandeert dat StartTime/EndTime UTC zijn
- `DateTime.UtcNow` is ook UTC
- Vergelijking is tijdzone-onafhankelijk

**Zonder ValueConverter zou dit fout gaan:**
```csharp
// FOUT als StartTime lokale tijd is en now UTC is
var now = DateTime.UtcNow;        // 14:00 UTC
var startTime = DateTime.Local;   // 16:00 Local (= 14:00 UTC)
// Vergelijking faalt omdat DateTimeKind niet matcht
```

---

### 8.3 Monitoring van Verbindingen Flow

**MonitoringService.ExecuteAsync() - Hoofdloop**

```
1. Start achtergrond taak
   ↓
2. LOOP (elke PollSeconds seconden):
   ↓
3. ProcessStationsAsync()
   ↓
4. Maak scoped DbContext via IServiceProvider
   ↓
5. Controleer of er actieve TestSession is (UTC check)
   ↓
6a. Geen actieve toets:
    - Clear in-memory MAC set
    - Skip naar volgende poll
6b. Wel actieve toets:
    - Ga verder met monitoring
   ↓
7. IStationProvider.GetStationsAsync()
   ↓ Platform-specifiek
8a. Linux: iw dev wlan0 station dump
8b. Windows: Mock data
   ↓
9. Parse output naar List<Station> met MAC-adressen
   ↓
10. Vergelijk met vorige poll (in-memory HashSet):
    - NewConnections = CurrentMacs - PreviousMacs
    - Disconnections = PreviousMacs - CurrentMacs
   ↓
11. Voor elke nieuwe connectie:
    ↓
12. Zoek Student met MacAddress
   ↓
13a. Niet gevonden: Log warning en skip
13b. Wel gevonden:
    - Maak Connection entity (ConnectedAt = UtcNow)
    - Maak EventLog entity (EventType.Connected)
    - SaveChangesAsync()
    - Stuur SignalR event naar alle clients
   ↓
14. Voor elke disconnectie:
   ↓
15. Zoek Student met MacAddress
   ↓
16a. Niet gevonden: Log warning en skip
16b. Wel gevonden:
    - Zoek open Connection (DisconnectedAt == null)
    - Zet DisconnectedAt = UtcNow
    - Maak EventLog entity (EventType.Disconnected)
    - SaveChangesAsync()
    - Stuur SignalR event naar alle clients
   ↓
17. Update in-memory HashSet met huidige MACs
   ↓
18. Wait PollSeconds
   ↓
19. Terug naar stap 2
```

**Database operaties bij nieuwe connectie:**
```sql
-- Nieuwe connection
INSERT INTO Connections (StudentId, ConnectedAt, DisconnectedAt)
VALUES (1, '2024-03-26 14:30:00', NULL);

-- Event log
INSERT INTO Events (StudentId, TestSessionId, EventType, Timestamp)
VALUES (1, 5, 0, '2024-03-26 14:30:00');
```

**SignalR broadcast:**
```csharp
await _hubContext.Clients.All.SendAsync("status", new
{
    mac = "aa:bb:cc:dd:ee:ff",
    status = "connected",
    name = "Jan Jansen",
    testName = "Wiskunde Toets"
});
```

---

### 8.4 Dashboard Real-time Update Flow

```
1. Gebruiker opent Dashboard.razor
   ↓
2. OnInitializedAsync()
   ↓
3. DashboardService.GetTestSessions()
   ↓ Query alle toetsen
4. Toon lijst met toetsen
   ↓
5. Gebruiker selecteert toets
   ↓
6. DashboardService.GetStudentsWithStatus(testSessionId)
   ↓ Query studenten + open connections
7. Toon lijst met studenten en online status
   ↓
8. SignalR connectie naar /hubs/status
   ↓
9. Luister naar "status" events
   ↓
10. Event ontvangen van MonitoringService:
    {
      mac: "aa:bb:cc:dd:ee:ff",
      status: "connected",
      name: "Jan Jansen",
      testName: "Wiskunde Toets"
    }
   ↓
11. Update UI (voeg toe aan online list of verwijder)
   ↓
12. StateHasChanged() - Blazor re-render
```

---

## 9. Belangrijke Technische Aandachtspunten

### 9.1 UTC Gebruik in TestSession

#### Probleem: Tijdzone Ambiguïteit
SQLite slaat `DateTime` op als TEXT (ISO 8601), maar behoudt geen tijdzone-informatie:
```
Opgeslagen: "2024-03-26T14:30:00"
Is dit UTC? Local? Onbekend?
```

Als code `DateTime.UtcNow` vergelijkt met een database waarde zonder `DateTimeKind`, kan de vergelijking falen.

#### Oplossing: EF Core ValueConverter
```csharp
var utcConverter = new ValueConverter<DateTime, DateTime>(
    v => v.ToUniversalTime(),                          // Opslaan: altijd UTC
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));   // Lezen: markeer als UTC
```

**Wat dit doet:**
1. **Bij opslaan:** Converteer elke DateTime naar UTC, zelfs als het al UTC was (idempotent)
2. **Bij uitlezen:** Zet `DateTimeKind.Utc` flag op de DateTime
3. **Bij querying:** EF Core kan nu correct vergelijken met `DateTime.UtcNow`

#### Gevolgen voor Code

**✅ Correct: UTC queries**
```csharp
var now = DateTime.UtcNow;  // 14:00 UTC, Kind = Utc
var activeSession = await context.TestSessions
    .Where(ts => ts.StartTime <= now && ts.EndTime >= now)
    .FirstOrDefaultAsync();
// StartTime heeft Kind = Utc door ValueConverter
// Vergelijking werkt correct
```

**❌ Fout: Local tijd queries**
```csharp
var now = DateTime.Now;  // 16:00 Local (CET), Kind = Local
var activeSession = await context.TestSessions
    .Where(ts => ts.StartTime <= now)
    .FirstOrDefaultAsync();
// StartTime heeft Kind = Utc
// DateTime vergelijkt Local vs UTC ZONDER conversie
// Resultaat is onvoorspelbaar
```

**✅ Correct: UI weergave**
```csharp
<p>@activeSession.StartTime.ToLocalTime().ToString("dd-MM-yyyy HH:mm")</p>
```
- `.ToLocalTime()` converteert UTC naar de lokale tijdzone van de server
- ToString() formatteert voor weergave

**✅ Correct: Invoer verwerken**
```csharp
// datetime-local geeft unspecified DateTime
var startTimeLocal = DateTime.Now;  // Van UI, Kind = Unspecified

// Expliciet markeren als lokale tijd en converteren naar UTC
var startUtc = DateTime.SpecifyKind(startTimeLocal, DateTimeKind.Local).ToUniversalTime();

session.StartTime = startUtc;  // ValueConverter slaat op als UTC
```

---

### 9.2 SQLite / EF Core Aandachtspunten

#### Type Mappings
SQLite heeft beperkte type ondersteuning:
- `INTEGER` - int, long, bool
- `REAL` - float, double
- `TEXT` - string, DateTime, DateTimeOffset
- `BLOB` - byte[]

**DateTime opslag:**
```sql
-- SQLite slaat op als TEXT in ISO 8601
CREATE TABLE TestSessions (
    Id INTEGER PRIMARY KEY,
    StartTime TEXT NOT NULL,
    EndTime TEXT NOT NULL
);

-- Voorbeeld data
INSERT INTO TestSessions VALUES (1, '2024-03-26T14:30:00', '2024-03-26T16:30:00');
```

**EF Core conversie:**
- Bij opslaan: `DateTime` → ISO 8601 string
- Bij lezen: ISO 8601 string → `DateTime`

#### Migrations
SQLite heeft beperkingen voor schema wijzigingen:
- Geen `ALTER COLUMN` support
- Geen `DROP COLUMN` support
- Wel `ADD COLUMN` support

**EF Core workaround:**
Bij complexe migraties maakt EF Core een nieuwe tabel en kopieert data over:
```sql
-- EF Core gegenereerde migration
CREATE TABLE TestSessions_Temp (...);
INSERT INTO TestSessions_Temp SELECT * FROM TestSessions;
DROP TABLE TestSessions;
ALTER TABLE TestSessions_Temp RENAME TO TestSessions;
```

#### Performance
**Unieke index op Student.MacAddress:**
```csharp
b.Entity<Student>().HasIndex(x => x.MacAddress).IsUnique();
```

Zonder deze index zou elke MAC-adres lookup een table scan vereisen:
```sql
-- Zonder index: O(n) - scant alle rijen
SELECT * FROM Students WHERE MacAddress = 'aa:bb:cc:dd:ee:ff';

-- Met index: O(log n) - gebruikt B-tree
```

MonitoringService doet dit elke poll voor elke nieuwe connectie, dus de index is essentieel.

#### Connection Pooling
SQLite ondersteunt geen echte connection pooling, maar EF Core simuleert dit:
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
```

**Default behavior:**
- Nieuwe `DbContext` per scope (per request in Blazor Server)
- MonitoringService maakt expliciete scopes via `IServiceProvider`

#### Concurrency
SQLite gebruikt file-level locking:
- Alleen één writer tegelijk
- Meerdere readers tegelijk

**Gevolg voor dit project:**
- MonitoringService schrijft elke N seconden
- Dashboard leest elke render
- Geen concurrency problemen verwacht bij deze load

Bij hogere load zou migratie naar PostgreSQL of SQL Server nodig zijn.

---

### 9.3 Verschil tussen Development en Productie

#### Conditional Compilation

```csharp
#if WINDOWS
    builder.Services.AddSingleton<IStationProvider, MockStationProvider>();
    builder.Services.AddSingleton<IMacResolver, WindowsMacResolver>();
#else
    builder.Services.AddSingleton<IStationProvider, LinuxIwStationProvider>();
    builder.Services.AddSingleton<IMacResolver, LinuxMacResolver>();
#endif
```

**Wanneer WINDOWS defined is:**
- Visual Studio Windows build
- `dotnet build` op Windows

**Wanneer WINDOWS NIET defined is:**
- `dotnet build` op Linux
- Deployment op Raspberry Pi

#### Environment-based Gedrag

**Development mode:**
```json
// appsettings.Development.json
{
  "DevelopmentTesting": {
    "EnableMockMacAddress": true,
    "MockMacAddress": "aa:bb:cc:dd:ee:ff"
  }
}
```

**Effecten:**
1. `DevelopmentMacResolverDecorator` gebruikt mock MAC op localhost
2. DetailedErrors enabled in ASP.NET Core
3. Developer exception page

**Production mode:**
```json
// appsettings.json
{
  "DevelopmentTesting": {
    "EnableMockMacAddress": false
  }
}
```

**Effecten:**
1. Mock MAC fallback is DISABLED (ook al staat het in config)
2. `Environment.IsDevelopment()` returns false
3. Exception handling middleware actief
4. HSTS enabled

#### Detectie in Code
```csharp
// Decorator controleert environment
if (!_environment.IsDevelopment())
{
    return null;  // Geen fallback in productie
}
```

**Deployment verschil:**
```bash
# Development
dotnet run --project StudentWifiMonitoring.Web
# Environment = Development (van launchSettings.json)

# Production
dotnet StudentWifiMonitoring.Web.dll
# Environment = Production (default)

# Of expliciet:
ASPNETCORE_ENVIRONMENT=Production dotnet StudentWifiMonitoring.Web.dll
```

#### Logging Levels
**Development:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Production (aanbevolen):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error",
      "StudentWifiMonitoring": "Information"
    }
  }
}
```

---

## 10. Deployment Overwegingen

### Raspberry Pi Deployment

**Vereisten:**
- .NET 8 Runtime (`dotnet-runtime-8.0`)
- SQLite3
- WiFi adapter in AP (Access Point) mode of monitoring mode
- `iw` command-line tool

**Stappen:**
1. Publish applicatie:
```bash
dotnet publish -c Release -o ./publish
```

2. Kopieer naar Raspberry Pi:
```bash
scp -r ./publish pi@raspberrypi:/home/pi/StudentWifiMonitoring
```

3. Configureer appsettings.json:
```json
{
  "ConnectionStrings": {
    "Default": "Data Source=/home/pi/StudentWifiMonitoring/app.db"
  },
  "Monitoring": {
    "PollSeconds": 2
  }
}
```

4. Run migrations:
```bash
dotnet ef database update --project StudentWifiMonitoring.Web
```

5. Start applicatie:
```bash
cd /home/pi/StudentWifiMonitoring
dotnet StudentWifiMonitoring.Web.dll
```

### systemd Service (TODO)
Nog niet geïmplementeerd, maar aanbevolen voor productie:

```ini
[Unit]
Description=Student WiFi Monitoring Service
After=network.target

[Service]
Type=notify
User=pi
WorkingDirectory=/home/pi/StudentWifiMonitoring
ExecStart=/usr/bin/dotnet /home/pi/StudentWifiMonitoring/StudentWifiMonitoring.Web.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

---

## 11. Testing Strategie

### Huidige Status
Het project heeft momenteel **geen** unit tests. Dit is acceptabel voor een MVP, maar voor productie wordt aanbevolen om tests toe te voegen.

### Aanbevolen Test Structuur

```
StudentWifiMonitoring.Tests/
├── Services/
│   ├── DashboardServiceTests.cs
│   ├── MonitoringServiceTests.cs
│   └── DevelopmentMacResolverDecoratorTests.cs
├── Integration/
│   ├── RegistrationFlowTests.cs
│   └── MonitoringFlowTests.cs
└── TestHelpers/
    ├── InMemoryDbContextFactory.cs
    └── MockStationProvider.cs
```

---

## 12. Bekende Beperkingen en Toekomstige Verbeteringen

### Huidige Beperkingen

1. **Geen authenticatie/autorisatie**
   - Elke gebruiker kan toetsen beheren
   - Geen rol-based access control

2. **Geen audit logging**
   - Wie heeft een toets aangemaakt?
   - Wie heeft data gewijzigd?

3. **Geen data export**
   - Geen CSV/Excel export van events
   - Geen rapportage functionaliteit

4. **Eenvoudige foutafhandeling**
   - Generieke foutmeldingen
   - Geen retry logica bij database fouten

5. **Geen real-time notificaties voor docenten**
   - Alleen dashboard updates
   - Geen email/SMS alerts bij disconnecties

---

## 13. Troubleshooting

### Probleem: Actieve toets wordt niet gedetecteerd

**Symptoom:** Register.razor toont "Geen actieve toets" terwijl er wel een toets aangemaakt is.

**Mogelijke oorzaken:**
1. Tijdzone mismatch
   - Check: Zijn StartTime/EndTime correct opgeslagen als UTC?
   - Fix: Gebruik de UTC ValueConverter in AppDbContext

2. Toets tijden zijn verstreken
   - Check: `SELECT * FROM TestSessions WHERE EndTime > datetime('now')`
   - Fix: Maak nieuwe toets met toekomstige eindtijd

3. Database niet gemigreerd
   - Check: `dotnet ef migrations list`
   - Fix: `dotnet ef database update`

**Debug query:**
```sql
SELECT 
    Name,
    StartTime,
    EndTime,
    datetime('now') as NowUtc,
    CASE 
        WHEN StartTime <= datetime('now') AND EndTime >= datetime('now') 
        THEN 'ACTIVE' 
        ELSE 'INACTIVE' 
    END as Status
FROM TestSessions;
```

### Probleem: MAC-adres kan niet bepaald worden

**Symptoom:** Register.razor toont "Kan je apparaat niet identificeren".

**Mogelijke oorzaken:**
1. Client is localhost
   - Expected: Localhost heeft geen MAC in ARP table
   - Fix: Development fallback moet enabled zijn

2. ARP/neighbor table is leeg
   - Check Linux: `ip neigh show`
   - Check Windows: `arp -a`
   - Fix: Zorg dat client eerst netwerk traffic heeft gehad

3. Development fallback niet geconfigureerd
   - Check: `appsettings.Development.json` heeft `EnableMockMacAddress: true`
   - Fix: Voeg configuratie toe

**Debug steps:**
```bash
# Linux
sudo ip neigh show

# Windows
arp -a

# Check of IP in table staat
ip neigh show <client-ip>
```

### Probleem: MonitoringService detecteert geen connecties

**Symptoom:** Dashboard toont geen online studenten terwijl ze wel verbonden zijn.

**Mogelijke oorzaken:**
1. Geen actieve toets
   - Check: Is er een TestSession met StartTime <= UtcNow <= EndTime?
   - Fix: Maak actieve toets

2. Student niet geregistreerd
   - Check: `SELECT * FROM Students WHERE MacAddress = '<mac>'`
   - Fix: Student moet eerst registreren via /register

3. IStationProvider geeft geen data
   - Check logs: "Fout tijdens het verwerken van stations"
   - Fix Linux: Check `iw dev` output
   - Fix Windows: MockStationProvider moet correct MAC's returnen

4. Poll interval te lang
   - Check: `appsettings.json` → `Monitoring:PollSeconds`
   - Fix: Verlaag naar 2-5 seconden voor testing

---

## Conclusie

Dit architectuurdocument beschrijft de huidige implementatie van StudentWifiMonitoring. De applicatie volgt een pragmatische, gelaagde architectuur waarbij:

- **UI-logica gescheiden is van business logica**
- **Infrastructuur concerns apart staan van domein logica**
- **Development en productie gedrag duidelijk gescheiden zijn**
- **UTC tijden consistent gebruikt worden**
- **Platformonafhankelijke interfaces gebruikt worden waar mogelijk**

De architectuur is bewust simpel gehouden om onderhoud en uitbreidingen te vergemakkelijken, terwijl belangrijke principes zoals Separation of Concerns en Single Responsibility worden nageleefd.
