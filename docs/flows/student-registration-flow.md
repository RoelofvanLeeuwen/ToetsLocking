# Student Registratie Flow - Technische Documentatie

## Overzicht

De studentregistratieflow is het proces waarbij een student zich via zijn/haar apparaat registreert voor een actieve toets. Het systeem detecteert automatisch het MAC-adres van het apparaat en koppelt dit aan de studentnaam.

**Kernfunctionaliteit:**
- Automatische MAC-adres detectie via IP-adres
- Upsert gedrag: nieuwe student aanmaken of bestaande updaten
- Validatie van actieve toetssessie
- Development fallback voor localhost testing

**Betrokken technologieën:**
- Blazor Server Interactive mode
- EF Core met SQLite
- Platform-specifieke netwerk commands (ARP/IP neigh)
- Decorator pattern voor development fallback

---

## 1. Initiële Toegang tot de Applicatie

### 1.1 Navigatie naar Registratiepagina

**URL:** `https://{host}/register`

**Component:** `Register.razor` (located in `Components/Pages/`)

**Blazor Lifecycle:**
```csharp
protected override async Task OnInitializedAsync()
{
    await LoadActiveSessionAsync();
}
```

**Wat gebeurt er:**
1. Browser maakt WebSocket connectie met server (Blazor Server mode)
2. `OnInitializedAsync()` wordt getriggerd
3. Actieve toetssessie wordt opgehaald uit database
4. UI wordt gerenderd op basis van resultaat

### 1.2 Network Context

**Client informatie beschikbaar via HttpContext:**
- Remote IP Address (IPv4 of IPv6)
- User Agent
- Connection details

**Voorbeeld IP-adressen:**
```
Production (Raspberry Pi WiFi):
  - Client: 192.168.1.50
  - Server: 192.168.1.1 (Raspberry Pi AP)

Development (localhost):
  - Client: ::1 (IPv6 loopback)
  - Client: 127.0.0.1 (IPv4 loopback)
```

---

## 2. Actieve Toets Detectie

### 2.1 Database Query

**Methode:** `LoadActiveSessionAsync()` in `Register.razor`

**Query logica:**
```csharp
var now = DateTime.UtcNow;
_activeSession = await DbContext.TestSessions
    .Where(ts => ts.StartTime <= now && ts.EndTime >= now)
    .OrderBy(ts => ts.StartTime)
    .FirstOrDefaultAsync();
```

**SQL equivalent:**
```sql
SELECT * FROM TestSessions
WHERE StartTime <= datetime('now') AND EndTime >= datetime('now')
ORDER BY StartTime ASC
LIMIT 1;
```

### 2.2 UTC Tijdzone Handling

**Waarom UTC belangrijk is:**
- SQLite slaat DateTime op als TEXT (ISO 8601)
- Zonder expliciete `DateTimeKind` zijn vergelijkingen onbetrouwbaar
- ValueConverter in `AppDbContext` garandeert UTC consistency

**ValueConverter configuratie:**
```csharp
// In AppDbContext.OnModelCreating
var utcConverter = new ValueConverter<DateTime, DateTime>(
    v => v.ToUniversalTime(),              // Opslaan: converteer naar UTC
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)); // Lezen: markeer als UTC

b.Entity<TestSession>()
    .Property(e => e.StartTime)
    .HasConversion(utcConverter);

b.Entity<TestSession>()
    .Property(e => e.EndTime)
    .HasConversion(utcConverter);
```

**Resultaat:**
- `DateTime.UtcNow` (Kind = Utc) vergelijkt correct met database waarden (Kind = Utc)
- Tijdzone conversie problemen zijn geëlimineerd

### 2.3 Scenario's

#### Scenario A: Actieve Toets Gevonden
```
Toets: "Wiskunde Eindtoets"
StartTime: 2024-03-26 13:00:00 UTC
EndTime: 2024-03-26 15:00:00 UTC
Current Time: 2024-03-26 14:00:00 UTC

Result: Toets is actief
UI: Registratieformulier wordt getoond
```

#### Scenario B: Geen Actieve Toets
```
Current Time: 2024-03-26 16:00:00 UTC
Toets A: EndTime = 2024-03-26 15:00:00 UTC (verstreken)
Toets B: StartTime = 2024-03-27 09:00:00 UTC (toekomstig)

Result: Geen actieve toets
UI: Waarschuwing "Er is momenteel geen toets actief"
```

### 2.4 UI Rendering

**Wanneer geen actieve toets:**
```html
<div class="alert alert-warning mt-3">
    <strong>Geen actieve toets</strong>
    <p class="mb-0">Er is momenteel geen toets actief. Registratie is alleen mogelijk tijdens een actieve toets.</p>
</div>
```

**Wanneer wel actieve toets:**
```html
<div class="card mt-3">
    <div class="card-body">
        <h5 class="card-title">Toets: Wiskunde Eindtoets</h5>
        <p class="text-muted">Looptijd: 26-03-2024 14:00 - 26-03-2024 16:00</p>
        <!-- Registratieformulier -->
    </div>
</div>
```

**Tijdzone conversie voor UI:**
```csharp
@_activeSession.StartTime.ToLocalTime().ToString("dd-MM-yyyy HH:mm")
```
- Server tijdzone (Raspberry Pi): Europe/Amsterdam (CET/CEST)
- `.ToLocalTime()` converteert UTC naar lokale tijd
- Gebruiker ziet lokale tijd, database blijft UTC

---

## 3. MAC-adres Resolutie

### 3.1 Architectuur Overzicht

**Decorator Pattern Stack:**
```
Register.razor
    ↓ inject IMacResolver
DevelopmentMacResolverDecorator
    ↓ wraps
WindowsMacResolver (Windows) / LinuxMacResolver (Linux)
    ↓ executes
Shell command: arp / ip neigh
    ↓ reads
ARP Table / Neighbor Table
```

### 3.2 Client IP Detectie

**Code in Register.razor:**
```csharp
var httpContext = HttpContextAccessor.HttpContext;
if (httpContext?.Connection?.RemoteIpAddress == null)
{
    Logger.LogWarning("Kan client IP-adres niet bepalen tijdens registratie");
    _errorMessage = "Kan je IP-adres niet bepalen. Neem contact op met de docent.";
    return;
}

var clientIp = httpContext.Connection.RemoteIpAddress.ToString();
```

**Voorbeelden:**
```
Production:
  RemoteIpAddress: 192.168.1.50

Development:
  RemoteIpAddress: ::1 (IPv6)
  RemoteIpAddress: 127.0.0.1 (IPv4)
```

### 3.3 Platform-Specifieke Resolvers

#### WindowsMacResolver (Development)

**Shell command:**
```bash
arp -a 192.168.1.50
```

**Output voorbeeld:**
```
Interface: 192.168.1.1 --- 0x3
  Internet Address      Physical Address      Type
  192.168.1.50          d4-3b-04-1a-2c-5e     dynamic
```

**Parsing logica:**
```csharp
public string? GetMacForIp(string ipAddress)
{
    var psi = new ProcessStartInfo
    {
        FileName = "arp",
        Arguments = "-a " + ipAddress,
        RedirectStandardOutput = true,
        UseShellExecute = false
    };

    using var p = Process.Start(psi);
    var output = p.StandardOutput.ReadToEnd();
    p.WaitForExit();

    var lines = output.Split('\n');
    foreach (var line in lines)
    {
        if (line.Contains(ipAddress))
        {
            var cols = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (cols.Length >= 2)
            {
                // Converteer "d4-3b-04-1a-2c-5e" naar "d4:3b:04:1a:2c:5e"
                return cols[1].Replace('-', ':').ToLowerInvariant();
            }
        }
    }
    return null;
}
```

#### LinuxMacResolver (Production)

**Shell command:**
```bash
ip neigh show 192.168.1.50
```

**Output voorbeeld:**
```
192.168.1.50 dev wlan0 lladdr d4:3b:04:1a:2c:5e REACHABLE
```

**Parsing logica:**
```csharp
public string? GetMacForIp(string ipAddress)
{
    var psi = new ProcessStartInfo
    {
        FileName = "/usr/sbin/ip",
        ArgumentList = { "neigh", "show", ipAddress },
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    using var p = Process.Start(psi);
    var output = p.StandardOutput.ReadToEnd();
    p.WaitForExit();

    // Output: "192.168.1.50 dev wlan0 lladdr d4:3b:04:1a:2c:5e REACHABLE"
    var parts = output.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var idx = Array.IndexOf(parts, "lladdr");
    if (idx > -1 && idx + 1 < parts.Length)
    {
        return parts[idx + 1].ToLowerInvariant();
    }
    return null;
}
```

### 3.4 MAC Format Normalisatie

**Verschillende formats:**
```
Windows ARP output: D4-3B-04-1A-2C-5E
Linux IP output:    d4:3b:04:1a:2c:5e
Database storage:   d4:3b:04:1a:2c:5e (lowercase, colons)
```

**Normalisatie:**
- `.Replace('-', ':')` - Converteer Windows format
- `.ToLowerInvariant()` - Lowercase voor consistency
- Database unique constraint op lowercase MAC

---

## 4. Development Fallback Mechanisme

### 4.1 Probleem op Localhost

**Waarom localhost geen MAC heeft:**
```bash
# Windows
C:\> arp -a 127.0.0.1
# Output: Geen entry (loopback is virtueel)

# Linux
$ ip neigh show ::1
# Output: Leeg (loopback is virtueel)
```

**Impact:**
- `WindowsMacResolver.GetMacForIp("127.0.0.1")` → `null`
- `LinuxMacResolver.GetMacForIp("::1")` → `null`
- Zonder fallback: registratie werkt niet op localhost

### 4.2 Decorator Implementatie

**Klasse:** `DevelopmentMacResolverDecorator`
**Locatie:** `Services/DevelopmentMacResolverDecorator.cs`

**Constructor dependencies:**
```csharp
public DevelopmentMacResolverDecorator(
    IMacResolver innerResolver,           // WindowsMacResolver / LinuxMacResolver
    IWebHostEnvironment environment,      // Development / Production check
    IConfiguration configuration,         // Lees appsettings
    ILogger<DevelopmentMacResolverDecorator> logger)
```

**Resolutie logica:**
```csharp
public string? GetMacForIp(string ipAddress)
{
    // Stap 1: Probeer echte resolver
    var macAddress = _innerResolver.GetMacForIp(ipAddress);
    
    // Stap 2: Als MAC gevonden, return direct
    if (!string.IsNullOrEmpty(macAddress))
    {
        return macAddress;
    }
    
    // Stap 3: Development fallback
    if (!_environment.IsDevelopment())
    {
        return null;  // Geen fallback in productie
    }
    
    var enableMockMac = _configuration.GetValue<bool>("DevelopmentTesting:EnableMockMacAddress", false);
    if (!enableMockMac)
    {
        return null;  // Fallback is uitgeschakeld
    }
    
    var mockMacAddress = _configuration.GetValue<string>("DevelopmentTesting:MockMacAddress");
    if (string.IsNullOrEmpty(mockMacAddress))
    {
        _logger.LogWarning("DevelopmentTesting:EnableMockMacAddress is true, maar MockMacAddress is niet geconfigureerd");
        return null;
    }
    
    _logger.LogWarning("Development mode: Gebruik mock MAC-adres {MockMac} voor IP {IpAddress}", 
        mockMacAddress, ipAddress);
    
    return mockMacAddress;
}
```

### 4.3 Configuratie

**appsettings.Development.json:**
```json
{
  "DevelopmentTesting": {
    "EnableMockMacAddress": true,
    "MockMacAddress": "aa:bb:cc:dd:ee:ff"
  }
}
```

**appsettings.json (productie):**
```json
{
  "DevelopmentTesting": {
    "EnableMockMacAddress": false
  }
}
```

### 4.4 Veiligheidsgaranties

**Drie-laags beveiliging:**
1. **Environment check:** `IsDevelopment()` moet `true` zijn
2. **Feature flag:** `EnableMockMacAddress` moet `true` zijn
3. **Configuration:** `MockMacAddress` moet ingevuld zijn

**Productie scenario:**
```
Environment: Production
EnableMockMacAddress: true (per ongeluk in config gelaten)

Result: Fallback wordt NIET gebruikt
Reden: IsDevelopment() = false
```

### 4.5 Dependency Injection Setup

**Program.cs registratie:**
```csharp
#if WINDOWS
    // Registreer inner resolver
    builder.Services.AddSingleton<WindowsMacResolver>();
    
    // Registreer decorator als IMacResolver
    builder.Services.AddSingleton<IMacResolver>(sp =>
    {
        var innerResolver = sp.GetRequiredService<WindowsMacResolver>();
        var environment = sp.GetRequiredService<IWebHostEnvironment>();
        var configuration = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILogger<DevelopmentMacResolverDecorator>>();
        return new DevelopmentMacResolverDecorator(innerResolver, environment, configuration, logger);
    });
#else
    builder.Services.AddSingleton<LinuxMacResolver>();
    builder.Services.AddSingleton<IMacResolver>(sp => { /* ... */ });
#endif
```

**Caller perspectief:**
```csharp
// Register.razor
@inject IMacResolver MacResolver

@code {
    // Component weet niet of dit een decorator of echte resolver is
    var mac = MacResolver.GetMacForIp(clientIp);
}
```

---

## 5. Student Opslag in Database

### 5.1 Database Schema

**Student tabel:**
```sql
CREATE TABLE Students (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    TestName TEXT NOT NULL,
    MacAddress TEXT NOT NULL UNIQUE
);

CREATE UNIQUE INDEX IX_Students_MacAddress ON Students (MacAddress);
```

**Domain model:**
```csharp
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
}
```

### 5.2 Upsert Logica

**Code in Register.razor:**
```csharp
// Zoek bestaande student op basis van MAC-adres
var existingStudent = await DbContext.Students
    .FirstOrDefaultAsync(s => s.MacAddress == macAddress);

if (existingStudent == null)
{
    // Scenario A: Nieuwe student
    var newStudent = new Student
    {
        Name = _studentName.Trim(),
        TestName = _activeSession.Name,
        MacAddress = macAddress
    };
    DbContext.Students.Add(newStudent);
    Logger.LogInformation("Nieuwe student geregistreerd: {StudentName} voor toets {TestName}", 
        newStudent.Name, _activeSession.Name);
}
else
{
    // Scenario B: Bestaande student
    existingStudent.Name = _studentName.Trim();
    existingStudent.TestName = _activeSession.Name;
    Logger.LogInformation("Bestaande student bijgewerkt: {StudentName} voor toets {TestName}", 
        existingStudent.Name, _activeSession.Name);
}

await DbContext.SaveChangesAsync();
```

### 5.3 Use Cases

#### Use Case 1: Eerste Registratie
```
Input:
  Name: "Jan Jansen"
  MAC: "aa:bb:cc:dd:ee:ff"
  Active Test: "Wiskunde Eindtoets"

Database BEFORE:
  Students table: (empty)

SQL Executed:
  INSERT INTO Students (Name, TestName, MacAddress)
  VALUES ('Jan Jansen', 'Wiskunde Eindtoets', 'aa:bb:cc:dd:ee:ff');

Database AFTER:
  Id | Name        | TestName             | MacAddress
  1  | Jan Jansen  | Wiskunde Eindtoets   | aa:bb:cc:dd:ee:ff
```

#### Use Case 2: Herregistratie (Zelfde Toets)
```
Scenario: Student registreert opnieuw voor dezelfde toets (typo in naam corrigeren)

Input:
  Name: "Jan Jansen (groep A)"
  MAC: "aa:bb:cc:dd:ee:ff"
  Active Test: "Wiskunde Eindtoets"

Database BEFORE:
  Id | Name        | TestName             | MacAddress
  1  | Jan Jansen  | Wiskunde Eindtoets   | aa:bb:cc:dd:ee:ff

SQL Executed:
  UPDATE Students
  SET Name = 'Jan Jansen (groep A)', TestName = 'Wiskunde Eindtoets'
  WHERE MacAddress = 'aa:bb:cc:dd:ee:ff';

Database AFTER:
  Id | Name                  | TestName             | MacAddress
  1  | Jan Jansen (groep A)  | Wiskunde Eindtoets   | aa:bb:cc:dd:ee:ff
```

#### Use Case 3: Herregistratie (Andere Toets)
```
Scenario: Dezelfde student registreert voor een andere toets

Input:
  Name: "Jan Jansen"
  MAC: "aa:bb:cc:dd:ee:ff"
  Active Test: "Engels Eindtoets"

Database BEFORE:
  Id | Name        | TestName             | MacAddress
  1  | Jan Jansen  | Wiskunde Eindtoets   | aa:bb:cc:dd:ee:ff

SQL Executed:
  UPDATE Students
  SET Name = 'Jan Jansen', TestName = 'Engels Eindtoets'
  WHERE MacAddress = 'aa:bb:cc:dd:ee:ff';

Database AFTER:
  Id | Name        | TestName           | MacAddress
  1  | Jan Jansen  | Engels Eindtoets   | aa:bb:cc:dd:ee:ff

Note: Oude TestName wordt overschreven (geen historisch record)
```

### 5.4 Waarom Denormalisatie van TestName?

**Reden:**
- Snelle filtering in Dashboard: `WHERE TestName = 'Wiskunde Eindtoets'`
- Geen JOIN met TestSession nodig
- SQLite performance optimalisatie

**Trade-off:**
- ✅ Betere read performance
- ✅ Simpelere queries
- ❌ Data inconsistency mogelijk (als TestSession.Name wijzigt)
- ❌ Geen historisch record van eerdere toetsen

**Alternatief (niet geïmplementeerd):**
```csharp
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CurrentTestSessionId { get; set; }  // FK naar TestSession
    public TestSession CurrentTestSession { get; set; }
}
```

---

## 6. Foutscenario's en Error Handling

### 6.1 Geen Actieve Toets

**Wanneer:**
- Geen TestSession met `StartTime <= UtcNow <= EndTime`

**UI Response:**
```html
<div class="alert alert-warning mt-3">
    <strong>Geen actieve toets</strong>
    <p class="mb-0">Er is momenteel geen toets actief. Registratie is alleen mogelijk tijdens een actieve toets.</p>
</div>
```

**Code:**
```csharp
if (_activeSession == null)
{
    // Registratieformulier wordt niet getoond
    return;
}
```

**Logging:**
- Geen logging (expected behavior)

---

### 6.2 Lege Naam Invoer

**Client-side validatie:**
```csharp
disabled="@(_isProcessing || string.IsNullOrWhiteSpace(_studentName))"
```
- Knop is disabled als naam leeg is

**Server-side validatie:**
```csharp
if (string.IsNullOrWhiteSpace(_studentName))
{
    _errorMessage = "Voer een geldige naam in.";
    return;
}
```

**UI Response:**
```html
<div class="alert alert-danger mt-3">
    <strong>Fout</strong>
    <p class="mb-0">Voer een geldige naam in.</p>
</div>
```

**Logging:**
- Geen logging (user input fout)

---

### 6.3 Client IP Niet Beschikbaar

**Wanneer:**
- `HttpContext` is `null` (zeer onwaarschijnlijk in Blazor Server)
- `HttpContext.Connection` is `null`
- `RemoteIpAddress` is `null`

**Code:**
```csharp
var httpContext = HttpContextAccessor.HttpContext;
if (httpContext?.Connection?.RemoteIpAddress == null)
{
    Logger.LogWarning("Kan client IP-adres niet bepalen tijdens registratie");
    _errorMessage = "Kan je IP-adres niet bepalen. Neem contact op met de docent.";
    return;
}
```

**UI Response:**
```
Fout: Kan je IP-adres niet bepalen. Neem contact op met de docent.
```

**Logging:**
```
[Warning] Kan client IP-adres niet bepalen tijdens registratie
```

**Mogelijke oorzaken:**
- Proxy misconfiguratie
- Reverse proxy zonder X-Forwarded-For headers
- Server/client communicatie problemen

**Troubleshooting:**
```csharp
// Voeg debug logging toe
Logger.LogInformation("HttpContext: {IsNull}", httpContext == null);
Logger.LogInformation("Connection: {IsNull}", httpContext?.Connection == null);
Logger.LogInformation("RemoteIpAddress: {IsNull}", httpContext?.Connection?.RemoteIpAddress == null);
```

---

### 6.4 MAC-adres Kan Niet Worden Bepaald

#### Scenario A: ARP/Neighbor Table Leeg (Productie)

**Wanneer:**
- Client heeft nog geen netwerk traffic gehad
- ARP cache is verlopen
- Firewall blokkeert ARP

**Code:**
```csharp
var macAddress = MacResolver.GetMacForIp(clientIp);
if (string.IsNullOrEmpty(macAddress))
{
    Logger.LogWarning("Kan MAC-adres niet bepalen voor IP {ClientIp}", clientIp);
    _errorMessage = "Kan je apparaat niet identificeren. Zorg dat je verbonden bent met het juiste WiFi-netwerk.";
    return;
}
```

**UI Response:**
```
Fout: Kan je apparaat niet identificeren. Zorg dat je verbonden bent met het juiste WiFi-netwerk.
```

**Logging:**
```
[Warning] Kan MAC-adres niet bepalen voor IP 192.168.1.50
```

**Oplossing voor gebruiker:**
1. Ping de Raspberry Pi: `ping 192.168.1.1`
2. Ververs de pagina
3. Probeer opnieuw te registreren

**Technische oplossing (server-side):**
```bash
# Forceer ARP entry (Linux)
sudo arping -c 1 192.168.1.50
```

#### Scenario B: Localhost zonder Development Fallback

**Wanneer:**
- Client IP is `127.0.0.1` of `::1`
- `EnableMockMacAddress` is `false` of niet geconfigureerd
- Development mode is aan

**Code flow:**
```
WindowsMacResolver.GetMacForIp("127.0.0.1") → null
  ↓
DevelopmentMacResolverDecorator.GetMacForIp("127.0.0.1")
  ↓ IsDevelopment() = true
  ↓ EnableMockMacAddress = false
  ↓
Return null
```

**UI Response:**
```
Fout: Kan je apparaat niet identificeren. Zorg dat je verbonden bent met het juiste WiFi-netwerk.
```

**Logging:**
```
[Warning] Kan MAC-adres niet bepalen voor IP ::1
```

**Oplossing:**
- Zet `EnableMockMacAddress` op `true` in `appsettings.Development.json`
- Configureer `MockMacAddress`

---

### 6.5 Database Errors

#### Scenario A: Unique Constraint Violation (Theoretisch Onmogelijk)

**Wanneer:**
- Race condition: twee requests met zelfde MAC tegelijkertijd
- Database corrupted

**SQL Error:**
```
SQLite Error 19: UNIQUE constraint failed: Students.MacAddress
```

**Code:**
```csharp
try
{
    await DbContext.SaveChangesAsync();
    _successMessage = $"Je bent succesvol geregistreerd voor '{_activeSession.Name}'.";
}
catch (Exception ex)
{
    Logger.LogError(ex, "Onverwachte fout tijdens registratie van student");
    _errorMessage = "Er is een onverwachte fout opgetreden. Probeer het opnieuw of neem contact op met de docent.";
}
```

**UI Response:**
```
Fout: Er is een onverwachte fout opgetreden. Probeer het opnieuw of neem contact op met de docent.
```

**Logging:**
```
[Error] Onverwachte fout tijdens registratie van student
Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes.
  ---> Microsoft.Data.Sqlite.SqliteException (0x80004005): SQLite Error 19: 'UNIQUE constraint failed: Students.MacAddress'.
```

**Waarom dit niet kan:**
- Upsert logica checkt eerst `FirstOrDefaultAsync(s => s.MacAddress == macAddress)`
- Blazor Server is single-threaded per circuit
- Race condition is onwaarschijnlijk

#### Scenario B: Database Locked

**Wanneer:**
- Andere proces heeft write lock op SQLite database
- Lange transactie in MonitoringService

**SQL Error:**
```
SQLite Error 5: database is locked
```

**Code:**
- Zelfde try/catch als boven

**UI Response:**
```
Fout: Er is een onverwachte fout opgetreden. Probeer het opnieuw of neem contact op met de docent.
```

**Logging:**
```
[Error] Onverwachte fout tijdens registratie van student
Microsoft.Data.Sqlite.SqliteException (0x80004005): SQLite Error 5: 'database is locked'.
```

**Mitigatie:**
- EF Core heeft retry logic (niet geïmplementeerd in dit project)
- SQLite gebruikt file-level locking (limitatie)
- Bij hogere load: migreer naar PostgreSQL/SQL Server

---

### 6.6 Blazor Circuit Disconnected

**Wanneer:**
- Netwerk verlies tijdens registratie
- Server herstart
- Idle timeout (default 3 minuten)

**Symptoom:**
- Knop reageert niet meer
- Spinner blijft draaien
- Console error: "Blazor SignalR connection lost"

**Blazor gedrag:**
- Automatic reconnection attempts (tot 8 keer)
- UI toont "Attempting to reconnect" banner

**Impact op registratie:**
- Als `SaveChangesAsync()` al succesvol was: student IS opgeslagen
- Als `SaveChangesAsync()` nog niet aangeroepen: student NIET opgeslagen
- `_isProcessing = false` in `finally` blok wordt niet uitgevoerd

**Gebruiker actie:**
- Wacht op reconnect
- Refresh de pagina
- Probeer opnieuw (upsert logica zorgt dat dit veilig is)

---

## 7. Betrokken Database Entiteiten

### 7.1 TestSession

**Verantwoordelijkheid:**
- Definiëren van actieve toetsperiode
- Naam van de toets

**Relaties:**
- 1-N naar `EventLog` (alle events tijdens deze toets)

**Query tijdens registratie:**
```sql
SELECT * FROM TestSessions
WHERE StartTime <= datetime('now') AND EndTime >= datetime('now')
ORDER BY StartTime ASC
LIMIT 1;
```

**Voorbeeld data:**
```
Id | Name                  | StartTime              | EndTime
1  | Wiskunde Eindtoets    | 2024-03-26T13:00:00Z  | 2024-03-26T15:00:00Z
2  | Engels Eindtoets      | 2024-03-27T09:00:00Z  | 2024-03-27T11:00:00Z
```

---

### 7.2 Student

**Verantwoordelijkheid:**
- Opslag van studentnaam
- Koppeling MAC-adres aan student
- Huidige toetsnaam (denormalized)

**Relaties:**
- 1-N naar `Connection` (alle verbindingen van deze student)
- 1-N naar `EventLog` (alle events van deze student)

**Constraints:**
- UNIQUE op `MacAddress`

**Lifecycle tijdens registratie:**
```
1. Lookup: FirstOrDefaultAsync(s => s.MacAddress == macAddress)
2a. Niet gevonden: INSERT nieuwe student
2b. Wel gevonden: UPDATE naam en testname
3. SaveChangesAsync()
```

**Voorbeeld data:**
```
Id | Name                | TestName             | MacAddress
1  | Jan Jansen          | Wiskunde Eindtoets   | aa:bb:cc:dd:ee:ff
2  | Piet Pietersen      | Wiskunde Eindtoets   | 11:22:33:44:55:66
3  | Kees Keessen        | Engels Eindtoets     | ff:ee:dd:cc:bb:aa
```

---

### 7.3 Connection (Niet Gebruikt Tijdens Registratie)

**Verantwoordelijkheid:**
- Track wanneer student online/offline gaat
- Wordt ALLEEN aangemaakt door `MonitoringService`

**Relatie met registratie:**
- Registratie maakt GEEN `Connection` aan
- `MonitoringService` maakt `Connection` aan bij eerste detectie
- `Student` moet bestaan voordat `Connection` gemaakt kan worden

**Voorbeeld data:**
```
Id | StudentId | ConnectedAt            | DisconnectedAt
1  | 1         | 2024-03-26T13:05:00Z  | 2024-03-26T13:25:00Z
2  | 1         | 2024-03-26T13:30:00Z  | NULL                    <- Momenteel online
3  | 2         | 2024-03-26T13:10:00Z  | 2024-03-26T14:00:00Z
```

---

### 7.4 EventLog (Niet Gebruikt Tijdens Registratie)

**Verantwoordelijkheid:**
- Auditlog van connect/disconnect events
- Wordt ALLEEN aangemaakt door `MonitoringService`

**Relatie met registratie:**
- Registratie maakt GEEN `EventLog` aan
- `MonitoringService` maakt `EventLog` bij connect/disconnect
- Student moet bestaan en toets moet actief zijn

**Voorbeeld data:**
```
Id | StudentId | TestSessionId | EventType  | Timestamp
1  | 1         | 1             | 0          | 2024-03-26T13:05:00Z  <- Connected
2  | 1         | 1             | 1          | 2024-03-26T13:25:00Z  <- Disconnected
3  | 1         | 1             | 0          | 2024-03-26T13:30:00Z  <- Connected
4  | 2         | 1             | 0          | 2024-03-26T13:10:00Z  <- Connected
```

---

## 8. Betrokken Code Componenten

### 8.1 Register.razor

**Locatie:** `Components/Pages/Register.razor`
**Type:** Blazor Server Component

**Dependencies (via @inject):**
```csharp
@inject AppDbContext DbContext
@inject IHttpContextAccessor HttpContextAccessor
@inject IMacResolver MacResolver
@inject ILogger<Register> Logger
```

**Belangrijke methoden:**

#### OnInitializedAsync()
```csharp
protected override async Task OnInitializedAsync()
{
    await LoadActiveSessionAsync();
}
```
- Blazor lifecycle event
- Laadt actieve toets
- Bepaalt of formulier getoond wordt

#### LoadActiveSessionAsync()
```csharp
private async Task LoadActiveSessionAsync()
{
    var now = DateTime.UtcNow;
    _activeSession = await DbContext.TestSessions
        .Where(ts => ts.StartTime <= now && ts.EndTime >= now)
        .OrderBy(ts => ts.StartTime)
        .FirstOrDefaultAsync();
}
```
- Query naar database
- UTC tijdzone handling
- Zet `_activeSession` field

#### RegisterAsync()
```csharp
private async Task RegisterAsync()
{
    _errorMessage = string.Empty;
    _successMessage = string.Empty;
    _isProcessing = true;

    try
    {
        // 1. Valideer naam
        // 2. Check actieve sessie
        // 3. Haal client IP
        // 4. Resolve MAC
        // 5. Upsert student
        // 6. Save changes
        // 7. Toon succesmelding
    }
    catch (Exception ex)
    {
        // Log en toon generieke foutmelding
    }
    finally
    {
        _isProcessing = false;
    }
}
```
- Hoofdlogica voor registratie
- Try/catch/finally voor error handling
- State management (_isProcessing voor UI)

**State fields:**
```csharp
private TestSession? _activeSession;
private string _studentName = string.Empty;
private string _errorMessage = string.Empty;
private string _successMessage = string.Empty;
private bool _isProcessing = false;
```

---

### 8.2 AppDbContext

**Locatie:** `Data/AppDbContext.cs`
**Type:** EF Core DbContext

**DbSets:**
```csharp
public DbSet<Student> Students => Set<Student>();
public DbSet<Connection> Connections => Set<Connection>();
public DbSet<EventLog> Events => Set<EventLog>();
public DbSet<TestSession> TestSessions => Set<TestSession>();
```

**Relevante configuratie:**
```csharp
protected override void OnModelCreating(ModelBuilder b)
{
    // Unique index op MacAddress
    b.Entity<Student>().HasIndex(x => x.MacAddress).IsUnique();
    
    // UTC ValueConverter voor TestSession timestamps
    var utcConverter = new ValueConverter<DateTime, DateTime>(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    
    b.Entity<TestSession>()
        .Property(e => e.StartTime)
        .HasConversion(utcConverter);
    
    b.Entity<TestSession>()
        .Property(e => e.EndTime)
        .HasConversion(utcConverter);
}
```

**DI Registratie:**
```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
```

**Lifetime:**
- Scoped per HTTP request (Blazor circuit in dit geval)
- `Register.razor` krijgt eigen instance via DI

---

### 8.3 IMacResolver Interface

**Locatie:** `Services/MacResolver.cs`
**Type:** Interface

**Signature:**
```csharp
public interface IMacResolver
{
    string? GetMacForIp(string ipAddress);
}
```

**Implementations:**
1. `WindowsMacResolver` - Windows platform
2. `LinuxMacResolver` - Linux platform
3. `DevelopmentMacResolverDecorator` - Wrapper voor development fallback

**Conditional compilation:**
```csharp
#if WINDOWS
    // Registreer WindowsMacResolver
#else
    // Registreer LinuxMacResolver
#endif
```

---

### 8.4 DevelopmentMacResolverDecorator

**Locatie:** `Services/DevelopmentMacResolverDecorator.cs`
**Type:** Decorator Service

**Verantwoordelijkheid:**
- Wrap platform-specifieke resolver
- Bied development fallback voor localhost
- Transparant voor callers

**Constructor:**
```csharp
public DevelopmentMacResolverDecorator(
    IMacResolver innerResolver,
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ILogger<DevelopmentMacResolverDecorator> logger)
```

**DI Registratie:**
```csharp
builder.Services.AddSingleton<WindowsMacResolver>();

builder.Services.AddSingleton<IMacResolver>(sp =>
{
    var innerResolver = sp.GetRequiredService<WindowsMacResolver>();
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<DevelopmentMacResolverDecorator>>();
    return new DevelopmentMacResolverDecorator(innerResolver, environment, configuration, logger);
});
```

**Lifetime:**
- Singleton (één instance voor hele applicatie)
- Stateless (geen mutable fields)

---

### 8.5 WindowsMacResolver / LinuxMacResolver

**Locatie:** `Services/MacResolver.cs`
**Type:** Platform-specific Services

**WindowsMacResolver:**
```csharp
public class WindowsMacResolver : IMacResolver
{
    public string? GetMacForIp(string ipAddress)
    {
        // Execute: arp -a {ipAddress}
        // Parse output
        // Return MAC or null
    }
}
```

**LinuxMacResolver:**
```csharp
public class LinuxMacResolver : IMacResolver
{
    public string? GetMacForIp(string ipAddress)
    {
        // Execute: /usr/sbin/ip neigh show {ipAddress}
        // Parse output
        // Return MAC or null
    }
}
```

**Lifetime:**
- Singleton
- Stateless

---

### 8.6 Domain Models

#### Student.cs
```csharp
namespace StudentWifiMonitoring.Web.Domain;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
}
```

#### TestSession.cs
```csharp
namespace StudentWifiMonitoring.Web.Domain;

public class TestSession
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive => DateTime.UtcNow >= StartTime && DateTime.UtcNow <= EndTime;
}
```

---

## 9. Volledige Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│ 1. BROWSER REQUEST                                          │
│    GET https://raspberrypi/register                         │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. BLAZOR SERVER                                            │
│    - Create SignalR connection                              │
│    - Initialize Register.razor component                    │
│    - Call OnInitializedAsync()                              │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. LOAD ACTIVE SESSION                                      │
│    LoadActiveSessionAsync()                                 │
│      ├─ var now = DateTime.UtcNow                           │
│      ├─ Query: SELECT * FROM TestSessions                   │
│      │         WHERE StartTime <= now AND EndTime >= now    │
│      └─ Result: TestSession or null                         │
└────────────────────────┬────────────────────────────────────┘
                         │
         ┌───────────────┴───────────────┐
         │                               │
         ▼                               ▼
┌─────────────────────┐   ┌─────────────────────────────────┐
│ 3A. NO ACTIVE TEST  │   │ 3B. ACTIVE TEST FOUND           │
│ - Show warning      │   │ - Show test name                │
│ - Hide form         │   │ - Show registration form        │
└─────────────────────┘   └────────────┬────────────────────┘
                                       │
                                       ▼
                          ┌─────────────────────────────────┐
                          │ 4. USER INPUT                   │
                          │    - Enter name: "Jan Jansen"   │
                          │    - Click "Registreren"        │
                          └────────────┬────────────────────┘
                                       │
                                       ▼
                          ┌─────────────────────────────────┐
                          │ 5. REGISTER ASYNC                │
                          │    _isProcessing = true          │
                          └────────────┬────────────────────┘
                                       │
                                       ▼
                          ┌─────────────────────────────────┐
                          │ 6. VALIDATE NAME                 │
                          │    if (IsNullOrWhiteSpace)       │
                          │      ├─ YES: Show error          │
                          │      └─ NO: Continue             │
                          └────────────┬────────────────────┘
                                       │
                                       ▼
                          ┌─────────────────────────────────┐
                          │ 7. GET CLIENT IP                 │
                          │    HttpContextAccessor           │
                          │      .HttpContext                │
                          │      .Connection                 │
                          │      .RemoteIpAddress            │
                          │    Result: "192.168.1.50"        │
                          └────────────┬────────────────────┘
                                       │
                                       ▼
                          ┌─────────────────────────────────┐
                          │ 8. RESOLVE MAC ADDRESS           │
                          │    MacResolver.GetMacForIp(ip)   │
                          └────────────┬────────────────────┘
                                       │
            ┌──────────────────────────┴──────────────────────────┐
            │                                                      │
            ▼                                                      ▼
┌────────────────────────────┐                    ┌───────────────────────────┐
│ 8A. PRODUCTION PATH        │                    │ 8B. DEVELOPMENT PATH      │
│ WindowsMacResolver or      │                    │ DevelopmentMacResolver    │
│ LinuxMacResolver           │                    │ Decorator                 │
│   ├─ Execute: arp / ip     │                    │   ├─ Call inner resolver  │
│   ├─ Parse output          │                    │   ├─ If null AND dev:     │
│   └─ Return MAC or null    │                    │   │   return mock MAC     │
└──────────┬─────────────────┘                    │   └─ Else: return null    │
           │                                       └────────┬──────────────────┘
           │                                                │
           └────────────────────┬───────────────────────────┘
                                │
                                ▼
                   ┌─────────────────────────────────┐
                   │ 9. MAC RESULT CHECK              │
                   │    if (IsNullOrEmpty)            │
                   │      ├─ YES: Show error          │
                   │      └─ NO: Continue             │
                   └────────────┬────────────────────┘
                                │
                                ▼
                   ┌─────────────────────────────────┐
                   │ 10. DATABASE LOOKUP              │
                   │     var existingStudent =        │
                   │       await DbContext.Students   │
                   │         .FirstOrDefaultAsync(    │
                   │           s => s.MacAddress ==   │
                   │                  macAddress)     │
                   └────────────┬────────────────────┘
                                │
                 ┌──────────────┴──────────────┐
                 │                             │
                 ▼                             ▼
    ┌──────────────────────┐     ┌──────────────────────────┐
    │ 10A. NEW STUDENT     │     │ 10B. EXISTING STUDENT    │
    │ - Create new Student │     │ - Update Name            │
    │ - Set Name           │     │ - Update TestName        │
    │ - Set TestName       │     │ - Keep MacAddress        │
    │ - Set MacAddress     │     │                          │
    │ - Add to DbContext   │     └──────────┬───────────────┘
    └──────────┬───────────┘                │
               │                             │
               └──────────────┬──────────────┘
                              │
                              ▼
                 ┌─────────────────────────────────┐
                 │ 11. SAVE CHANGES                 │
                 │     await DbContext              │
                 │       .SaveChangesAsync()        │
                 │                                  │
                 │     SQL: INSERT or UPDATE        │
                 └────────────┬────────────────────┘
                              │
                              ▼
                 ┌─────────────────────────────────┐
                 │ 12. SUCCESS                      │
                 │     _successMessage = "..."      │
                 │     _studentName = ""            │
                 │     Logger.LogInformation(...)   │
                 └────────────┬────────────────────┘
                              │
                              ▼
                 ┌─────────────────────────────────┐
                 │ 13. FINALLY                      │
                 │     _isProcessing = false        │
                 └────────────┬────────────────────┘
                              │
                              ▼
                 ┌─────────────────────────────────┐
                 │ 14. UI UPDATE                    │
                 │     - Show success message       │
                 │     - Clear form                 │
                 │     - Re-render component        │
                 └──────────────────────────────────┘
```

---

## 10. Samenvatting

De studentregistratieflow is een kritisch onderdeel van de applicatie waarbij:

1. **Actieve toets detectie** gebeurt via UTC-consistente database queries
2. **MAC-adres resolutie** platform-onafhankelijk is via interface abstractie
3. **Development fallback** transparant werkt zonder UI-aanpassingen
4. **Database upsert** zorgt dat herregistratie veilig is
5. **Error handling** generiek is naar gebruiker, specifiek in logs

**Belangrijke design decisions:**
- Decorator pattern voor development concerns
- UTC ValueConverter voor tijdzone consistency
- Upsert logica voor herregistraties
- Denormalisatie van TestName voor performance
- Generieke error messages naar gebruiker
- Gedetailleerde logging voor debugging

**Future improvements:**
- Rate limiting (voorkom spam registraties)
- Email/SMS verificatie
- Multi-toets registratie (studenten kunnen voor meerdere toetsen tegelijk geregistreerd zijn)
- Historisch record van alle registraties (niet alleen huidige toets)
- Admin dashboard voor registratie beheer
