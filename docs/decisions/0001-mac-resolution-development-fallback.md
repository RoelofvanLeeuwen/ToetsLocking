# ADR-0001: Development Fallback voor MAC-resolutie op Infrastructuurniveau

## Status

**Geaccepteerd** - 2024

## Context

### Probleem

Tijdens lokale ontwikkeling op Windows/localhost werkt MAC-adres resolutie niet omdat:

1. **Localhost heeft geen MAC-adres in ARP/neighbor table**
   - `127.0.0.1` en `::1` zijn virtuele loopback interfaces
   - `arp -a 127.0.0.1` en `ip neigh show ::1` geven geen resultaat
   - Dit is verwacht gedrag, geen bug

2. **Studentregistratie is afhankelijk van MAC-adres**
   - Register.razor vereist een MAC-adres om een student op te slaan
   - Zonder MAC-adres kan registratie niet werken
   - Dit maakt lokale ontwikkeling en testing onmogelijk

3. **Development vs Production requirements**
   - In productie (Raspberry Pi): MAC-resolutie MOET echt werken
   - In development (localhost): MAC-resolutie MOET mock support hebben
   - Deze twee eisen zijn tegenstrijdig zonder expliciete scheiding

### Technische Constraint

**Platform-specifieke implementaties:**
- Windows: `WindowsMacResolver` gebruikt `arp -a`
- Linux: `LinuxMacResolver` gebruikt `ip neigh show`
- Beide returnen `null` voor localhost

**Blazor Server context:**
- Multiple components kunnen MAC-resolutie nodig hebben (Register.razor, toekomstige componenten)
- Development-specifieke logica in elke component is code duplication
- UI-componenten moeten platform-agnostisch blijven

### Stakeholder Requirements

1. **Ontwikkelaars:** Moeten lokaal kunnen testen zonder fysiek WiFi netwerk
2. **Productie:** Moet nooit mock data gebruiken
3. **Codebase:** Moet onderhoudbaar en uitbreidbaar blijven
4. **Architectuur:** Moet layered architectuur respecteren (UI, Service, Infrastructure)

---

## Beslissing

**We implementeren een `DevelopmentMacResolverDecorator` die de platform-specifieke resolvers wrapt en een development-only fallback biedt.**

### Implementatie

```csharp
public class DevelopmentMacResolverDecorator : IMacResolver
{
    private readonly IMacResolver _innerResolver;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevelopmentMacResolverDecorator> _logger;

    public string? GetMacForIp(string ipAddress)
    {
        // 1. Probeer echte resolver
        var macAddress = _innerResolver.GetMacForIp(ipAddress);
        
        // 2. Als MAC gevonden, return direct
        if (!string.IsNullOrEmpty(macAddress))
            return macAddress;
        
        // 3. Development fallback (alleen in Development mode)
        if (!_environment.IsDevelopment())
            return null;
        
        if (!_configuration.GetValue<bool>("DevelopmentTesting:EnableMockMacAddress"))
            return null;
        
        var mockMac = _configuration.GetValue<string>("DevelopmentTesting:MockMacAddress");
        _logger.LogWarning("Development mode: Gebruik mock MAC {MockMac} voor IP {Ip}", 
            mockMac, ipAddress);
        
        return mockMac;
    }
}
```

### Dependency Injection Setup

```csharp
// Program.cs
#if WINDOWS
    builder.Services.AddSingleton<WindowsMacResolver>();
    builder.Services.AddSingleton<IMacResolver>(sp =>
    {
        var innerResolver = sp.GetRequiredService<WindowsMacResolver>();
        var environment = sp.GetRequiredService<IWebHostEnvironment>();
        var configuration = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILogger<DevelopmentMacResolverDecorator>>();
        return new DevelopmentMacResolverDecorator(innerResolver, environment, configuration, logger);
    });
#endif
```

### Configuratie

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

### Component Usage (ongewijzigd)

```csharp
// Register.razor
@inject IMacResolver MacResolver

@code {
    private async Task RegisterAsync()
    {
        // Component weet niet of dit een decorator of echte resolver is
        var macAddress = MacResolver.GetMacForIp(clientIp);
        
        // Geen environment checks nodig
        // Geen configuratie toegang nodig
        // Gewoon gebruiken als interface
    }
}
```

---

## Gevolgen

### Positieve Gevolgen

#### 1. Separation of Concerns

**UI Laag blijft clean:**
- `Register.razor` kent geen development vs production verschil
- Geen environment checks in Blazor componenten
- Geen configuratie-logica in UI code

**Voorbeeld VOOR deze beslissing (afgewezen):**
```csharp
// Register.razor - VERKEERD
@inject IConfiguration Configuration
@inject IWebHostEnvironment Environment

@code {
    var mac = MacResolver.GetMacForIp(ip);
    
    // Development logica in UI component - SLECHT
    if (string.IsNullOrEmpty(mac) && Environment.IsDevelopment())
    {
        if (Configuration["DevelopmentTesting:EnableMockMacAddress"] == "true")
            mac = Configuration["DevelopmentTesting:MockMacAddress"];
    }
}
```

**Voorbeeld NA deze beslissing (geaccepteerd):**
```csharp
// Register.razor - CORRECT
@inject IMacResolver MacResolver

@code {
    var mac = MacResolver.GetMacForIp(ip);
    // Klaar. Geen extra logica nodig.
}
```

#### 2. Single Responsibility Principle

**Elke class heeft één verantwoordelijkheid:**

| Class | Verantwoordelijkheid |
|-------|---------------------|
| `Register.razor` | UI rendering en user input handling |
| `WindowsMacResolver` | Windows ARP table uitlezen |
| `LinuxMacResolver` | Linux neighbor table uitlezen |
| `DevelopmentMacResolverDecorator` | Development fallback logica |

**Geen mixed concerns:**
- UI components kennen geen infrastructuur details
- Platform resolvers kennen geen environment logic
- Decorator kent geen UI state

#### 3. Open/Closed Principle

**Extensible zonder modificatie:**

Nieuwe component die MAC-resolutie nodig heeft:
```csharp
// DeviceManagement.razor - NIEUWE component
@inject IMacResolver MacResolver

@code {
    // Krijgt automatisch development fallback
    // Geen extra code nodig
    var mac = MacResolver.GetMacForIp(ip);
}
```

Geen wijzigingen in:
- `DevelopmentMacResolverDecorator` (al geïmplementeerd)
- `WindowsMacResolver` / `LinuxMacResolver` (ongewijzigd)
- `Program.cs` DI setup (al geconfigureerd)

#### 4. DRY (Don't Repeat Yourself)

**Zonder decorator (code duplication):**
```csharp
// Register.razor
var mac = MacResolver.GetMacForIp(ip);
if (string.IsNullOrEmpty(mac) && IsDevelopment) { /* fallback */ }

// DeviceManagement.razor
var mac = MacResolver.GetMacForIp(ip);
if (string.IsNullOrEmpty(mac) && IsDevelopment) { /* fallback */ }

// StudentStatus.razor
var mac = MacResolver.GetMacForIp(ip);
if (string.IsNullOrEmpty(mac) && IsDevelopment) { /* fallback */ }
```

**Met decorator (single source of truth):**
```csharp
// Alle components
var mac = MacResolver.GetMacForIp(ip);
// Fallback zit in één plek: DevelopmentMacResolverDecorator
```

#### 5. Testbaarheid

**Unit test voor decorator:**
```csharp
[Fact]
public void GetMacForIp_InProduction_WithNoMac_ReturnsNull()
{
    // Arrange
    var mockResolver = new Mock<IMacResolver>();
    mockResolver.Setup(r => r.GetMacForIp("192.168.1.1")).Returns((string?)null);
    
    var mockEnv = new Mock<IWebHostEnvironment>();
    mockEnv.Setup(e => e.EnvironmentName).Returns("Production");
    
    var decorator = new DevelopmentMacResolverDecorator(
        mockResolver.Object, mockEnv.Object, config, logger);
    
    // Act
    var result = decorator.GetMacForIp("192.168.1.1");
    
    // Assert
    Assert.Null(result);  // Geen fallback in productie
}

[Fact]
public void GetMacForIp_InDevelopment_WithNoMac_ReturnsMockMac()
{
    // Arrange
    var mockResolver = new Mock<IMacResolver>();
    mockResolver.Setup(r => r.GetMacForIp("127.0.0.1")).Returns((string?)null);
    
    var mockEnv = new Mock<IWebHostEnvironment>();
    mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
    
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["DevelopmentTesting:EnableMockMacAddress"] = "true",
            ["DevelopmentTesting:MockMacAddress"] = "test:mac:address"
        })
        .Build();
    
    var decorator = new DevelopmentMacResolverDecorator(
        mockResolver.Object, mockEnv.Object, config, logger);
    
    // Act
    var result = decorator.GetMacForIp("127.0.0.1");
    
    // Assert
    Assert.Equal("test:mac:address", result);
}
```

**Blazor component testing blijft simpel:**
```csharp
// Register.razor test - mock IMacResolver direct
var mockResolver = new Mock<IMacResolver>();
mockResolver.Setup(r => r.GetMacForIp(It.IsAny<string>())).Returns("aa:bb:cc:dd:ee:ff");

// Geen environment mocking nodig
// Geen configuratie mocking nodig
```

#### 6. Productie Veiligheid

**Drie-laags beveiliging tegen onbedoelde mock data in productie:**

```csharp
// Laag 1: Environment check
if (!_environment.IsDevelopment())
    return null;

// Laag 2: Feature flag
if (!_configuration.GetValue<bool>("DevelopmentTesting:EnableMockMacAddress"))
    return null;

// Laag 3: Configuratie check
var mockMac = _configuration.GetValue<string>("DevelopmentTesting:MockMacAddress");
if (string.IsNullOrEmpty(mockMac))
    return null;
```

**Scenario: Configuratie per ongeluk in productie:**
```json
// appsettings.json (productie server)
{
  "DevelopmentTesting": {
    "EnableMockMacAddress": true,  // OEPS!
    "MockMacAddress": "aa:bb:cc:dd:ee:ff"
  }
}
```

**Resultaat:**
- `IsDevelopment()` = `false` (productie environment)
- Decorator returnt `null` (Laag 1 security check)
- Mock MAC wordt NIET gebruikt
- Productie blijft veilig

#### 7. Configuratie Flexibiliteit

**Per developer aanpasbaar:**

Developer A (wil lokaal testen):
```json
{
  "DevelopmentTesting": {
    "EnableMockMacAddress": true,
    "MockMacAddress": "11:22:33:44:55:66"
  }
}
```

Developer B (test met echt netwerk):
```json
{
  "DevelopmentTesting": {
    "EnableMockMacAddress": false
  }
}
```

**Geen code changes nodig.**

#### 8. Logging en Auditbaarheid

**Elke keer dat mock mode gebruikt wordt:**
```
[Warning] Development mode: Gebruik mock MAC-adres aa:bb:cc:dd:ee:ff voor IP ::1
```

**Productie logs tonen nooit deze warning:**
- Duidelijk onderscheid tussen dev en prod gedrag
- Debugging wordt makkelijker
- Audit trail is transparant

---

### Negatieve Gevolgen

#### 1. Extra Abstraction Layer

**Complexity:**
- Één extra class in codebase
- Decorator pattern moet begrepen worden
- DI setup is complexer

**Mitigatie:**
- Decorator is well-known pattern
- Documentatie in architecture.md en ADR
- Code is goed gecommentarieerd

#### 2. Indirectie in Dependency Resolution

**Stack trace bij debugging:**
```
Register.razor
  → IMacResolver (interface)
    → DevelopmentMacResolverDecorator
      → WindowsMacResolver
        → Process.Start("arp")
```

Versus direct:
```
Register.razor
  → WindowsMacResolver
    → Process.Start("arp")
```

**Mitigatie:**
- Logging op elk niveau
- Stack traces zijn nog steeds leesbaar
- Voordelen overtreffen dit nadeel

#### 3. Configuratie Dependency

**Vereist:**
- `appsettings.Development.json` moet correct ingevuld zijn
- Ontwikkelaars moeten weten dat deze configuratie bestaat

**Mitigatie:**
- appsettings.Development.json is in Git
- README bevat setup instructies
- First-time setup is eenmalig

---

## Alternatieven die zijn Overwogen

### Alternatief 1: Development Logica in Register.razor

**Implementatie:**
```csharp
// Register.razor
@inject IConfiguration Configuration
@inject IWebHostEnvironment Environment

@code {
    private async Task RegisterAsync()
    {
        var mac = MacResolver.GetMacForIp(clientIp);
        
        // Development fallback in component
        if (string.IsNullOrEmpty(mac))
        {
            if (Environment.IsDevelopment() && 
                Configuration.GetValue<bool>("DevelopmentTesting:EnableMockMacAddress"))
            {
                mac = Configuration.GetValue<string>("DevelopmentTesting:MockMacAddress");
            }
        }
        
        if (string.IsNullOrEmpty(mac))
        {
            _errorMessage = "Kan apparaat niet identificeren.";
            return;
        }
        
        // Rest van registratie logica
    }
}
```

**Voordelen:**
- ✅ Simpel te begrijpen
- ✅ Geen extra classes
- ✅ Directe code flow

**Nadelen:**
- ❌ **Violation of Single Responsibility** - UI component heeft infrastructuur kennis
- ❌ **Code duplication** - Elke component die MAC-resolutie nodig heeft moet dezelfde logica dupliceren
- ❌ **Moeilijk testbaar** - Moet Environment en Configuration mocken in component tests
- ❌ **Separation of Concerns** - Development concern zit in UI laag
- ❌ **Open/Closed violation** - Nieuwe components vereisen dezelfde fallback code

**Waarom afgewezen:**
- Breekt fundamentele architectuurprincipes
- Schaalt niet bij meer components
- Verhoogt onderhoudskosten significant

---

### Alternatief 2: Conditional IMacResolver Registratie

**Implementatie:**
```csharp
// Program.cs
#if WINDOWS
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddSingleton<IMacResolver, MockMacResolver>();
    }
    else
    {
        builder.Services.AddSingleton<IMacResolver, WindowsMacResolver>();
    }
#endif
```

```csharp
// MockMacResolver.cs
public class MockMacResolver : IMacResolver
{
    private readonly IConfiguration _configuration;
    
    public string? GetMacForIp(string ipAddress)
    {
        return _configuration.GetValue<string>("DevelopmentTesting:MockMacAddress");
    }
}
```

**Voordelen:**
- ✅ Components blijven clean
- ✅ Geen decorator nodig
- ✅ Simpele DI setup

**Nadelen:**
- ❌ **Geen graceful degradation** - Als echte resolver werkt in development, wordt die niet gebruikt
- ❌ **All-or-nothing** - Kan niet switchen tussen echt en mock gedrag
- ❌ **Test moeilijker** - Kan niet testen of productie resolver werkt op development machine
- ❌ **Loss of real resolver** - WindowsMacResolver wordt nooit gebruikt in development

**Waarom afgewezen:**
- Te rigide: kan niet testen met echte resolver in development
- Geen fallback gedrag: altijd mock OF altijd echt
- Verliest voordeel van decorator pattern (try real, fallback to mock)

---

### Alternatief 3: Environment-based Configuration met Factory

**Implementatie:**
```csharp
// Program.cs
builder.Services.AddSingleton<IMacResolver, MacResolverFactory>();

public class MacResolverFactory : IMacResolver
{
    private readonly IMacResolver _resolver;
    
    public MacResolverFactory(IWebHostEnvironment env, IConfiguration config)
    {
        if (env.IsDevelopment() && config.GetValue<bool>("UseMockMac"))
        {
            _resolver = new MockMacResolver(config);
        }
        else
        {
            #if WINDOWS
                _resolver = new WindowsMacResolver();
            #else
                _resolver = new LinuxMacResolver();
            #endif
        }
    }
    
    public string? GetMacForIp(string ipAddress)
    {
        return _resolver.GetMacForIp(ipAddress);
    }
}
```

**Voordelen:**
- ✅ Centraal decision point
- ✅ Components blijven clean

**Nadelen:**
- ❌ **Geen graceful degradation** - Zelfde probleem als Alternatief 2
- ❌ **Factory complexity** - Factory is eigenlijk een decorator in disguise
- ❌ **Conditional compilation in factory** - Maakt factory platform-afhankelijk

**Waarom afgewezen:**
- Dezelfde nadelen als Alternatief 2
- Factory voegt geen waarde toe boven decorator
- Decorator pattern is expliciter en idiomatischer

---

### Alternatief 4: Development-only Middleware

**Implementatie:**
```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        // Inject fake MAC in HttpContext
        context.Items["MockMac"] = configuration["DevelopmentTesting:MockMacAddress"];
        await next();
    });
}

// Register.razor
var mockMac = HttpContext.Items["MockMac"] as string;
var mac = MacResolver.GetMacForIp(ip) ?? mockMac;
```

**Voordelen:**
- ✅ Middleware is development-only

**Nadelen:**
- ❌ **HttpContext pollution** - Items dictionary wordt misbruikt
- ❌ **UI awareness** - Component moet weten van Items["MockMac"]
- ❌ **Not type-safe** - String key en casting vereist
- ❌ **Unclear contract** - Geen interface definieert dit gedrag

**Waarom afgewezen:**
- Misbruikt HttpContext.Items (niet bedoeld voor dit doel)
- Geen compile-time safety
- Verbergt intentie in plaats van expliciet te maken

---

## Implementatie Details

### Benodigde Files

1. **Services/DevelopmentMacResolverDecorator.cs** (nieuw)
2. **Program.cs** (wijziging in DI registratie)
3. **appsettings.Development.json** (nieuwe configuratie sectie)

### Breaking Changes

**Geen breaking changes:**
- Bestaande `IMacResolver` interface blijft ongewijzigd
- Components die `IMacResolver` gebruiken blijven ongewijzigd
- Alleen DI registratie wijzigt

### Migration Path

**Stap 1:** Implementeer `DevelopmentMacResolverDecorator`
**Stap 2:** Update `Program.cs` DI registratie
**Stap 3:** Voeg configuratie toe aan `appsettings.Development.json`
**Stap 4:** Test in development mode
**Stap 5:** Test in productie mode (geen mock MAC verwacht)

### Rollback Strategy

Als decorator problemen veroorzaakt:

```csharp
// Program.cs - Rollback naar directe registratie
#if WINDOWS
    builder.Services.AddSingleton<IMacResolver, WindowsMacResolver>();
#else
    builder.Services.AddSingleton<IMacResolver, LinuxMacResolver>();
#endif
```

Geen code changes in components nodig - alleen DI setup.

---

## Referenties

- **Design Pattern:** Decorator Pattern (Gang of Four)
- **SOLID Principles:** 
  - Single Responsibility Principle
  - Open/Closed Principle
  - Dependency Inversion Principle
- **Architecture:** Clean Architecture (Robert C. Martin)
- **Testing:** Test-Driven Development best practices

## Gerelateerde Beslissingen

- ADR-0002: Platform-specifieke service implementaties (toekomstig)
- ADR-0003: Conditional compilation strategie (toekomstig)

## Wijzigingshistorie

| Datum | Versie | Wijziging | Auteur |
|-------|--------|-----------|--------|
| 2024  | 1.0    | Initiële versie | Development Team |

---

## Conclusie

De beslissing om development fallback op infrastructuurniveau te implementeren via een decorator pattern is de juiste architectuurkeuze omdat:

1. **Separation of Concerns** - Elk niveau heeft één verantwoordelijkheid
2. **DRY** - Geen code duplication in UI components
3. **Open/Closed** - Nieuwe components krijgen fallback gratis
4. **Testbaarheid** - Elke laag is onafhankelijk testbaar
5. **Productie veilig** - Drie-laags beveiliging tegen onbedoelde mock data
6. **Onderhoudbaar** - Single source of truth voor fallback logica

De extra complexity van een decorator class is een acceptabele trade-off voor deze voordelen.
