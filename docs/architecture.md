# StudentWifiMonitoring - Huidige Architectuur

## Doel en scope

Dit document beschrijft de actuele architectuur van de applicatie in de repository op **6 mei 2026**.

De actieve applicatie is:

- `StudentWifiMonitoring.Web`

De repository bevat ook oudere of afgeleide projecten zoals `ToetsLockingWifiMonitoring.Web`, `ToetsLocking.Client` en `ToetsLocking.Server`, maar die vormen niet de primaire runtime voor de huidige WiFi-monitoringoplossing.

De applicatie ondersteunt twee hoofdrollen:

- **studenten** die zich registreren voor een actieve toets en hun verbindingsstatus kunnen bekijken
- **docenten** die toetsen beheren, live status volgen en exports downloaden

## Kernfunctionaliteit

- Studentregistratie voor een actieve toets via `/register`
- Koppeling van een student aan een apparaat via MAC-adres
- Continue monitoring van verbonden apparaten tijdens een actieve toets
- Realtime updates via SignalR naar docentdashboard en studentstatusscherm
- Beheer van toetsen via `/tests`
- CSV-export van events via `/export` en `/export-by-test`
- Development-only simulatie van mock stations via `/dev/stations`

## Technologiestack

- `.NET 8`
- `ASP.NET Core` met `Razor Components`
- `Blazor Server` met `InteractiveServer`
- `Entity Framework Core` met `SQLite`
- `SignalR`
- Linux netwerktools in productie: `iw` en `ip`
- Windows development helpers: `arp` en `MockStationProvider`

Let op: het project target `net8.0`, maar gebruikt op dit moment EF Core packages `9.0.9`.

## Hoofdstructuur

```text
StudentWifiMonitoring.Web/
├── Components/
│   ├── Layout/
│   ├── Pages/
│   │   ├── Home.razor
│   │   ├── Register.razor
│   │   ├── MyScreen.razor
│   │   ├── Tests.razor
│   │   ├── Status.razor
│   │   ├── Export.razor
│   │   ├── ExportByTest.razor
│   │   ├── TeacherLogin.razor
│   │   └── DevStations.razor
│   └── Shared/
├── Data/
│   └── AppDbContext.cs
├── Domain/
│   ├── Student.cs
│   ├── TestSession.cs
│   ├── Connection.cs
│   ├── EventLog.cs
│   └── EventType.cs
├── DTOs/
│   ├── Dashboard/
│   ├── DevStations/
│   ├── Export/
│   ├── MyScreen/
│   ├── Status/
│   ├── Students/
│   └── Tests/
├── Hubs/
│   └── StatusHub.cs
├── Migrations/
├── Services/
│   ├── Interfaces/
│   ├── DashboardService.cs
│   ├── DevStationsService.cs
│   ├── ExportService.cs
│   ├── DevelopmentMacResolverDecorator.cs
│   ├── LinuxIwStationProvider.cs
│   ├── MacResolver.cs
│   ├── MockStationProvider.cs
│   ├── MonitoringService.cs
│   ├── MyScreenService.cs
│   ├── StatusService.cs
│   ├── StudentRegistrationService.cs
│   ├── TeacherAuthService.cs
│   └── TestManagementService.cs
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

## Runtime-opbouw

### 1. UI-laag

De UI bestaat uit interactieve Blazor Server pagina's.

Belangrijkste routes:

- `/register`: studentregistratie
- `/myscreen`: fullscreen verbindingsstatus van de huidige student
- `/`: docentdashboard met realtime status
- `/tests`: beheer van toetsen
- `/status`: eenvoudige lijst met momenteel verbonden studenten
- `/export`: export van alle events
- `/export-by-test`: export per toets
- `/teacher`: docentlogin/logout
- `/dev/stations`: development-only mockstationbeheer

### 2. Service-laag

De pagina's praten primair met services via interfaces. De service-laag bevat:

- businesslogica
- validatie
- querylogica
- mapping naar DTO's
- integratie met infrastructuur zoals `IMacResolver`, `IStationProvider` en `SignalR`

De geregistreerde servicecontracten in `Program.cs` zijn:

- `IDashboardService`
- `ITestManagementService`
- `IStudentRegistrationService`
- `IStatusService`
- `IMyScreenService`
- `IExportService`
- `IDevStationsService`
- `ITeacherAuthService`

### 3. Data-laag

`AppDbContext` beheert vier entiteiten:

- `Student`
- `TestSession`
- `Connection`
- `EventLog`

Belangrijke kenmerken:

- unieke index op `Student.MacAddress`
- foreign keys van `Connection -> Student`
- foreign keys van `EventLog -> Student` en `EventLog -> TestSession`
- UTC-conversie op `TestSession.StartTime` en `TestSession.EndTime`

### 4. Achtergrondverwerking

`MonitoringService` draait als hosted service en voert periodiek de monitoringlus uit:

1. zoek een actieve toets
2. lees huidige stations via `IStationProvider`
3. vergelijk de actuele set met de vorige poll
4. registreer connect/disconnect events
5. update open `Connection` records
6. broadcast statuswijzigingen via `StatusHub`

### 5. Realtime laag

`StatusHub` bevat geen custom hub-methodes. Het wordt uitsluitend gebruikt voor broadcasts vanuit services:

- `MonitoringService`
- `TestManagementService` bij het deactiveren van een toets

Clients die luisteren:

- `Home.razor`
- `MyScreen.razor`

`Status.razor` gebruikt geen SignalR, maar pollt elke 3 seconden via `PeriodicTimer`.

## Belangrijkste services

### StudentRegistrationService

Verantwoordelijkheden:

- actieve toets bepalen
- studentinput valideren
- MAC-adres bepalen via `IMacResolver`
- student upserten op basis van `MacAddress`
- resultaat teruggeven via DTO

De pagina `Register.razor` haalt alleen het client-IP op via `IHttpContextAccessor` en delegeert de rest aan deze service.

### TestManagementService

Verantwoordelijkheden:

- pagineren en filteren van toetsen
- aanmaken en wijzigen van toetsen
- lokale tijd uit de UI converteren naar UTC
- actieve toetsen bovenaan sorteren
- een toets deactiveren door `EndTime` terug te zetten
- open verbindingen sluiten en disconnect-events uitsturen

### DashboardService

Verantwoordelijkheden:

- lijst van toetsen leveren aan het dashboard
- studenten inclusief online/offline status leveren
- optioneel filteren op geselecteerde toets

De filtering op toets gebeurt op basis van `Student.TestName`, dus via een gededenormaliseerde naam en niet via een foreign key op `Student`.

### StatusService

Levert een eenvoudige lijst van alle studenten met een open `Connection`.

### MyScreenService

Levert de status van een individuele student:

- geregistreerd of niet
- momenteel verbonden of niet
- naam
- toetsnaam

### ExportService

Genereert CSV-inhoud in memory en schrijft geen serverbestanden weg.

### DevStationsService

Ondersteunt development-only acceptatietests:

- actieve toets ophalen
- teststudent aanmaken of bijwerken
- in combinatie met `MockStationProvider` volledige monitoringflow simuleren

### TeacherAuthService

Docentauthenticatie is bewust simpel gehouden:

- controle op een `HttpOnly` cookie
- login/logout via echte HTTP POST endpoints in `Program.cs`
- geen cookie-writes vanuit interactieve componenten

Dit voorkomt Blazor Server-problemen waarbij response headers al gestart zijn.

## Infrastructuurkeuzes

### IStationProvider

De station provider wordt **runtime-based** geregistreerd:

- op Windows: `MockStationProvider`
- op Linux: `LinuxIwStationProvider`

Gevolg:

- development kan zonder echte WiFi clients plaatsvinden
- in productie wordt station-informatie uit `iw dev <iface> station dump` gehaald

### IMacResolver

De MAC-resolver wordt geregistreerd als een decorator rondom een platformresolver:

- inner resolver: `WindowsMacResolver` of `LinuxMacResolver`
- outer resolver: `DevelopmentMacResolverDecorator`

Gedrag:

1. probeer echte MAC-resolutie
2. als dat mislukt en de app draait in `Development`
3. en `DevelopmentTesting:EnableMockMacAddress=true`
4. gebruik `DevelopmentTesting:MockMacAddress`

Dat is relevant voor:

- `/register`
- `/myscreen`

### Tijd- en timezonebeleid

Regels:

- opslag van `TestSession` tijden gebeurt als UTC
- queries op actieve toetsen gebruiken `DateTime.UtcNow`
- UI gebruikt lokale tijd voor invoer en presentatie
- `CreateTestAsync` en `UpdateTestAsync` converteren expliciet van local time naar UTC

### Database migraties

Bij elke startup voert `Program.cs` automatisch `Database.Migrate()` uit.

Dat betekent:

- een lege database wordt automatisch opgebouwd
- een bestaand schema wordt bijgewerkt naar de laatste migration

## UI-architectuurregels

### Huidige hoofdregel

De meeste pagina's gebruiken geen `AppDbContext` direct. Ze spreken services aan via interfaces en werken met DTO's.

### Belangrijke uitzondering

`MyScreen.razor` gebruikt naast `IMyScreenService` ook direct:

- `IMacResolver`
- `IHttpContextAccessor`
- `IWebHostEnvironment`

Dat is een bewuste uitzondering omdat die pagina eerst de identiteit van de huidige client moet bepalen voordat een servicequery kan worden gedaan. In development ondersteunt `MyScreen.razor` bovendien een querystring fallback `?mac=...`.

### Docentafscherming

Docentpagina's controleren lokaal `TeacherAuth.IsTeacher()`. Dat is UI-afscherming, geen volwaardige autorisatie. Voor release 1 is dat bewust voldoende, maar het is geen securitymodel op enterprise-niveau.

## Belangrijkste runtime-flows

### Studentregistratie

1. `Register.razor` laadt de actieve toets via `IStudentRegistrationService`
2. de pagina leest het client-IP
3. de service zoekt opnieuw de actieve toets
4. `IMacResolver` bepaalt het MAC-adres
5. student wordt aangemaakt of bijgewerkt op `MacAddress`
6. `MonitoringService` kan die student daarna herkennen bij connecties

### Live monitoring

1. `MonitoringService` pollt stations
2. nieuwe MAC-adressen leveren `Connection` en `EventLog` records op
3. verdwenen MAC-adressen sluiten open verbindingen en loggen disconnects
4. SignalR stuurt `"status"` events naar clients
5. dashboard en `MyScreen` werken hun UI direct bij

### Toets deactiveren

1. docent zet een actieve toets inactief via `/tests`
2. `TestManagementService` zet `EndTime` terug naar `UtcNow - 1s`
3. open verbindingen van studenten met dezelfde `TestName` worden gesloten
4. disconnect-events worden gelogd
5. SignalR verstuurt disconnect-notificaties

## Configuratie

Belangrijkste configuratiesleutels:

- `ConnectionStrings:Default`
- `Monitoring:Interface`
- `Monitoring:PollSeconds`
- `Teacher:Password`
- `ForceHttps`
- `DevelopmentTesting:EnableMockMacAddress`
- `DevelopmentTesting:MockMacAddress`

`ForceHttps` staat standaard uit, zodat de app op een Raspberry Pi zonder TLS-certificaat via HTTP kan draaien.

## Deploymentmodel

Het beoogde deploymentmodel is Docker op Raspberry Pi met:

- persistente SQLite-opslag
- host networking
- Linux netwerktools beschikbaar in de container
- monitoringinterface via configuratie, beoogd `eth1` op de Pi

Lokaal op Windows ligt de nadruk op functionele flowtests, niet op realistische netwerkmonitoring.

## Bekende beperkingen

- geen ASP.NET Identity of gebruikersdatabase
- docentauthenticatie is cookie + pincode, zonder rollenmodel
- `Student` verwijst niet via foreign key naar een toets; filtering gebeurt via `TestName`
- `Status.razor` pollt in plaats van SignalR te gebruiken
- monitoring is afhankelijk van de Linux netwerkinterface en van de beschikbaarheid van `iw` en `ip`
- de repository bevat nog legacy projecten, wat contextverwarring kan geven

## Samenvatting

De huidige architectuur is pragmatisch en service-gecentreerd:

- Blazor pagina's verzorgen UI en beperkte request-context
- services bevatten businesslogica, mapping en databasequeries
- `MonitoringService` beheert de live netwerkstatus
- SignalR verzorgt realtime distributie
- SQLite is de enige datastore
- development en productie verschillen bewust in de station provider en de MAC-resolutie fallback

Voor verdere verdieping:

- `docs/flows/student-registration-flow.md`
- `docs/decisions/0001-mac-resolution-development-fallback.md`
