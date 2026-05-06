# Studentregistratieflow

## Doel

Deze flow beschrijft de actuele registratieketen in `StudentWifiMonitoring.Web` op **6 mei 2026**. De registratieflow loopt nu via:

- `Register.razor`
- `IStudentRegistrationService` / `StudentRegistrationService`
- `IMacResolver`
- `AppDbContext`

De pagina schrijft dus niet meer zelf rechtstreeks naar de database.

## Betrokken componenten

### UI

- `Components/Pages/Register.razor`

### Services

- `IStudentRegistrationService`
- `StudentRegistrationService`
- `IMacResolver`
- `DevelopmentMacResolverDecorator`

### Data

- `AppDbContext`
- `Student`
- `TestSession`

## Functionele samenvatting

Een student opent `/register`, ziet alleen een formulier als er een actieve toets is, vult een naam in en laat de server het apparaat identificeren via het client-IP en het bijbehorende MAC-adres. Daarna wordt de student aangemaakt of bijgewerkt op basis van `MacAddress`.

## Stap voor stap

### 1. Pagina initialisatie

`Register.razor` roept tijdens `OnInitializedAsync()` aan:

```csharp
_activeSession = await RegistrationService.GetActiveTestSessionAsync();
```

De service queryt:

- huidige tijd via `DateTime.UtcNow`
- `TestSessions` waar `StartTime <= now && EndTime >= now`
- oudste actieve sessie eerst

Resultaat:

- **wel actieve toets**: formulier tonen
- **geen actieve toets**: waarschuwing tonen en registratie blokkeren

## 2. Student start registratie

De pagina verzamelt:

- `StudentName`
- client-IP via `IHttpContextAccessor`

Dat is de enige infrastructuurverantwoordelijkheid die bewust in de pagina blijft, omdat `HttpContext` daar direct beschikbaar is.

Daarna bouwt de pagina:

```csharp
new StudentRegistrationRequestDto
{
    StudentName = _studentName,
    ClientIpAddress = clientIp
}
```

en roept:

```csharp
await RegistrationService.RegisterStudentAsync(request);
```

## 3. Servicevalidatie

`StudentRegistrationService.RegisterStudentAsync` valideert in deze volgorde:

1. naam is niet leeg
2. er is nog steeds een actieve toets
3. client-IP is ingevuld

Belangrijk:

- de service controleert de actieve toets opnieuw
- een toets die tussen paginaladen en submit eindigt wordt dus correct afgewezen

## 4. MAC-resolutie

De service roept aan:

```csharp
var macAddress = _macResolver.GetMacForIp(request.ClientIpAddress);
```

### Productiepad

- Linux gebruikt `LinuxMacResolver`
- die voert `/usr/sbin/ip neigh show <ip>` uit

### Windows developmentpad

- Windows gebruikt `WindowsMacResolver`
- die voert `arp -a <ip>` uit

### Development fallback

Als de inner resolver niets vindt en de app draait in `Development`, dan kan `DevelopmentMacResolverDecorator` terugvallen op:

- `DevelopmentTesting:EnableMockMacAddress=true`
- `DevelopmentTesting:MockMacAddress=<mac>`

Dat maakt lokale registratie vanaf `localhost` mogelijk.

## 5. Upsert van student

Na succesvolle MAC-resolutie zoekt de service:

```csharp
await _db.Students.FirstOrDefaultAsync(s => s.MacAddress == macAddress);
```

### Scenario A: nieuwe student

De service maakt een nieuwe `Student` aan met:

- `Name`
- `TestName = actieve toets naam`
- `MacAddress`

### Scenario B: bestaande student

De service werkt alleen deze velden bij:

- `Name`
- `TestName`

`MacAddress` blijft de stabiele sleutel.

## 6. Opslag

De service voert uit:

```csharp
await _db.SaveChangesAsync();
```

Daarmee is de student geregistreerd. Er wordt in deze flow nog geen `Connection` of `EventLog` aangemaakt.

Dat gebeurt pas later in `MonitoringService` wanneer het apparaat ook echt als verbonden station wordt gezien.

## 7. Response naar de UI

De service retourneert `StudentRegistrationResultDto`.

### Bij succes

- `Success = true`
- succesmelding met toetsnaam

De pagina:

- toont de melding
- wist het naamveld

### Bij fout

- `Success = false`
- gebruikersvriendelijke foutmelding

De pagina toont die melding zonder de app te laten crashen.

## Relatie met monitoring

Registratie alleen is niet genoeg voor "online" status.

De vervolgstappen zijn:

1. `MonitoringService` pollt stations via `IStationProvider`
2. een gevonden MAC wordt gematcht met `Student.MacAddress`
3. dan pas worden `Connection` en `EventLog` records aangemaakt
4. daarna volgen realtime `"status"` events via SignalR

Dus:

- registratie maakt een student bekend
- monitoring bepaalt de echte live verbindingsstatus

## Foutscenario's

### Geen actieve toets

Gebruiker ziet:

- melding dat er geen actieve toets is

Technisch:

- `GetActiveTestSessionAsync()` geeft `null`

### Lege naam

Afgewezen in:

- UI voor snelle feedback
- service voor definitieve validatie

### Geen client-IP

Afgewezen in `Register.razor` voordat de service wordt aangeroepen.

### Geen MAC-adres gevonden

Afgewezen in `StudentRegistrationService`.

Mogelijke oorzaken:

- geen bruikbare entry in ARP/neighbor table
- development fallback uitgeschakeld
- student zit niet op het juiste netwerk

### Onverwachte database- of runtimefout

De service logt de exception en retourneert een generieke foutmelding.

## Architectuurpunten

- `Register.razor` gebruikt geen `AppDbContext` direct
- businesslogica en opslag zitten in `StudentRegistrationService`
- MAC-resolutie blijft infrastructuur en zit achter `IMacResolver`
- development fallback zit niet in de pagina maar in de decorator
- de flow gebruikt DTO's tussen UI en service

## Compact sequentieoverzicht

```text
Browser
  -> Register.razor
  -> StudentRegistrationService.GetActiveTestSessionAsync()
  -> AppDbContext.TestSessions

Browser submit
  -> Register.razor
  -> IHttpContextAccessor (client IP)
  -> StudentRegistrationService.RegisterStudentAsync()
  -> IMacResolver
  -> AppDbContext.Students
  -> SaveChangesAsync()
  -> StudentRegistrationResultDto
  -> Register.razor update UI
```

## Belangrijk verschil met oudere documentatie

De oude documentatie beschreef nog een flow waarin `Register.razor` direct met `AppDbContext` werkte. Dat is niet meer actueel. De huidige implementatie gebruikt een servicegerichte flow met:

- `StudentRegistrationService`
- DTO's
- gecentraliseerde validatie
- gecentraliseerde MAC-resolutie
