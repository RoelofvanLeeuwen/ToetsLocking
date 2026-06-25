# StudentWifiMonitoring

`StudentWifiMonitoring` is de primaire naam van deze applicatie. In deze repository komt ook nog oudere naamgeving zoals `ToetsLocking` of `ToetsLockingWifiMonitoring` voor. De actieve .NET webapp voor development en deployment is `StudentWifiMonitoring.Web`.

## Wat deze applicatie doet

StudentWifiMonitoring is een `.NET 8` Blazor Server applicatie voor toetsafname op een Raspberry Pi zonder internettoegang. Studenten verbinden met het netwerk van de Pi, registreren zich voor een actieve toets en worden daarna gemonitord op verbonden/verbroken netwerkstatus. Docenten kunnen toetsen beheren, live status bekijken en exports downloaden.

De applicatie is bedoeld voor:

- studenten die zich registreren op een toetsapparaat
- docenten die toetsen en live monitoring beheren
- beheerders/ontwikkelaars die de applicatie lokaal of op een Raspberry Pi draaien

## Hoofdfunctionaliteit

- Studentregistratie voor een actieve toets
- Koppeling tussen student en apparaat op basis van MAC-adres
- Live monitoring van connect/disconnect tijdens een actieve toets
- Bijhouden van verbindingshistorie per student: teller van verbroken verbindingen zichtbaar in dashboard, volledige activiteitenlog op klik
- Docentdashboard met statusinformatie via SignalR
- Toetsbeheer met zoeken, sorteren en paginatie
- CSV-export direct in memory, zonder bestanden op de server achter te laten
- SQLite-opslag met automatische migratie bij startup

## Architectuuroverzicht

De applicatie gebruikt een pragmatische tussenstructuur:

- Blazor-pagina's gebruiken geen `AppDbContext` direct
- UI werkt via services, interfaces en DTO's
- Businesslogica, queries, filtering, updates en mapping zitten in services
- Services gebruiken voorlopig direct `AppDbContext`
- Er is bewust nog geen repository-laag
- Er wordt geen AutoMapper gebruikt

Belangrijke mappen:

- `StudentWifiMonitoring.Web/Data` voor `AppDbContext`
- `StudentWifiMonitoring.Web/Domain` voor entities
- `StudentWifiMonitoring.Web/DTOs` voor UI/service DTO's
- `StudentWifiMonitoring.Web/Services` voor businesslogica
- `StudentWifiMonitoring.Web/Services/Interfaces` voor servicecontracten
- `StudentWifiMonitoring.Web/Components/Pages` voor Blazor-pagina's
- `StudentWifiMonitoring.Web/Hubs` voor SignalR
- `docs/` voor verdiepende documentatie

## Functionele gebieden

- `Register`: student registreert zich voor een actieve toets; MAC-resolutie gebeurt server-side via `IMacResolver`
- `MyScreen`: studentpagina met eigen status; identificatie hoort server-side te gebeuren
- `Home`: docentdashboard met live updates via SignalR
- `Tests`: toetsbeheer voor aanmaken, wijzigen, zoeken, sorteren en deactiveren
- `Status`: overzichtspagina voor statusinformatie
- `Export` en `ExportByTest`: CSV-downloads zonder server-side bestandsopslag
- `DevStations`: development-only hulppagina voor simulatie; mag niet bruikbaar zijn in productie

## Toegang en afscherming

Release 1 gebruikt een eenvoudige docentafscherming:

- studenten zien alleen studentrelevante pagina's
- docentfunctionaliteit is afgeschermd
- docenttoegang loopt via een verborgen loginroute met pincode
- de login gebruikt een HTTP/cookie-flow
- directe URL-toegang tot docentpagina's moet voor studenten geblokkeerd blijven
- `DevStations` is alleen bedoeld voor development

## Configuratie

De belangrijkste configuratie zit in `StudentWifiMonitoring.Web/appsettings.json` en kan via environment variables worden overschreven.

Belangrijke keys:

- `ConnectionStrings:Default`
- `Monitoring:Interface`
- `Monitoring:PollSeconds`
- `Teacher:Password`
- `ForceHttps`
- `AllowedHosts`

Belangrijke environment variables:

```text
ConnectionStrings__Default=Data Source=/data/app.db
Monitoring__Interface=eth1
Monitoring__PollSeconds=2
Teacher__Password=<pincode>
ForceHttps=false
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
```

Belangrijke deploymentafspraken:

- op de Raspberry Pi wordt geen HTTPS gebruikt
- `ForceHttps` moet op de Pi `false` zijn
- het docentwachtwoord moet in productie via environment variables overschreven worden
- de monitoringinterface op de Pi is beoogd `eth1`
- SQLite moet persistent op een Docker volume staan
- database migraties draaien automatisch bij startup via `Database.Migrate()`

## Lokaal starten

Vereisten:

- .NET 8 SDK
- Windows development is ondersteund voor UI en functionele flow
- Houd er rekening mee dat realistische netwerkmonitoring Linux-specifiek is

Applicatie lokaal starten:

```powershell
dotnet run --project StudentWifiMonitoring.Web
```

Wat lokaal relevant is:

- development gebruikt een MAC-fallback via `DevelopmentMacResolverDecorator`
- op Windows wordt een `MockStationProvider` gebruikt
- de app gebruikt standaard een lokale SQLite database (`Data Source=app.db`)

## Docker lokaal

Voor lokaal testen op Windows:

```powershell
docker compose -f docker-compose.local.yml up --build
```

Dit compose-bestand:

- start `StudentWifiMonitoring.Web` op `http://localhost:5000`
- gebruikt een Docker volume voor `/data/app.db`
- is geschikt voor functioneel testen van de app
- is niet geschikt voor realistische Linux netwerkmonitoring

## Docker op Raspberry Pi

Voor productie op de Pi:

```bash
docker compose -f docker-compose.pi.yml up --build -d
```

Belangrijk:

- `docker-compose.pi.yml` gebruikt `network_mode: host`
- de container luistert op poort `5000`
- SQLite draait via `ConnectionStrings__Default=Data Source=/data/app.db`
- persistente opslag loopt via een Docker volume
- `Teacher__Password` moet aangepast worden voor productie

## Releases en gepubliceerde images

Bij elke versietag op `main` wordt automatisch een Docker image gebouwd en gepubliceerd via GitHub Actions.

### Trigger

Push een tag in het formaat `vMAJOR.MINOR.PATCH` op de `main`-branch:

```bash
git tag v1.0.0
git push origin v1.0.0
```

### Gepubliceerde images

De workflow publiceert twee tags tegelijk naar `ghcr.io`:

| Tag | Beschrijving |
|-----|-------------|
| `ghcr.io/roelofvanleeuwen/gctoetslocking:1.0.0` | Versiespecifieke tag (zonder `v`-prefix) |
| `ghcr.io/roelofvanleeuwen/gctoetslocking:latest` | Altijd de laatste gepubliceerde versie |

De image is gebouwd voor `linux/arm64` (Raspberry Pi).

### Zichtbaarheid en authenticatie

De image is privaat. Ophalen vereist authenticatie met een geldig GitHub Personal Access Token (PAT) met minimaal de scope `read:packages`.

Inloggen op de server voordat je de image pullt:

```bash
echo <PAT> | docker login ghcr.io -u <github-gebruikersnaam> --password-stdin
```

Daarna pullen:

```bash
docker pull ghcr.io/roelofvanleeuwen/gctoetslocking:latest
```

### Image gebruiken op de Raspberry Pi

Vervang de lokale build in `docker-compose.pi.yml` door de gepubliceerde image:

```yaml
services:
  app:
    image: ghcr.io/roelofvanleeuwen/gctoetslocking:1.0.0
```

Zorg dat Docker op de Pi is ingelogd bij `ghcr.io` (zie authenticatie hierboven) voordat je `docker compose up` uitvoert.

## Huidige status en aandachtspunten

De UI-refactor is grotendeels afgerond:

- functionele Razor-pagina's gebruiken geen directe `AppDbContext`
- services, interfaces en DTO's zijn ingevoerd
- businesslogica en mapping zitten in services

De belangrijkste open validatiepunten liggen nu op runtime-niveau op de Raspberry Pi:

- werkt Docker stabiel op de Pi
- werkt `docker-compose.pi.yml` zoals bedoeld
- blijft SQLite persistent
- werken `iw` en `ip neigh` in de container
- ziet de container de juiste netwerkinterface
- werkt monitoring correct op `eth1`

Bekende beperkingen voor release 1:

- geen ASP.NET Identity of gebruikersdatabase
- geen instellingenpagina
- geen repository pattern in deze fase
- geen reverse proxy / HTTPS-oplossing
- focus op één container

## Documentatie

Gebruik de README als startpunt en de documenten in `docs/` voor detailinformatie:

- [Architectuur](docs/architecture.md)
- [Raspberry Pi deployment guide](docs/StudentWifiMonitoring_RaspberryPi_Deployment_Guide.md)
- [Student registration flow](docs/flows/student-registration-flow.md)
- [ADR: development fallback voor MAC-resolutie](docs/decisions/0001-mac-resolution-development-fallback.md)

### Branchplannen

- [feature/ip-info-en-powershell-helper](docs/plans/feature-ip-info-en-powershell-helper.md) — IP Ethernet-kaart tonen, PowerShell-script voor download, uitlegpagina voor docenten
- [feature/docker-publish-ghcr](docs/plans/feature-docker-publish-ghcr.md) — GitHub Actions workflow voor automatische Docker image publicatie op ghcr.io bij versietag

## Repository-opmerking

De repository bevat ook oudere projectresten zoals `ToetsLockingWifiMonitoring.Web` en een ouder `docker-compose.yml`. Voor de huidige applicatie en deployment moet je uitgaan van:

- `StudentWifiMonitoring.Web`
- `docker-compose.local.yml`
- `docker-compose.pi.yml`
