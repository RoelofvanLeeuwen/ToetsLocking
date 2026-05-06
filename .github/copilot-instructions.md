# Copilot Instructions

## Doel van deze repository

Deze repository bevat een .NET 8 / C# applicatie die draait op een Raspberry Pi en lokaal op Windows ontwikkeld kan worden.  

De applicatie gebruikt Blazor Server, EF Core met SQLite, SignalR en achtergrondservices voor netwerkmonitoring.

Copilot moet code voorstellen die:

- netjes genoeg is voor productie
- pragmatisch en onderhoudbaar blijft
- geen onnodige abstracties introduceert
- past binnen de bestaande projectstructuur van deze repository

## Algemene uitgangspunten

- Schrijf code in het Engels.
- UI-teksten mogen Nederlands zijn.
- Comments en documentatie zijn in het Nederlands.
- Genereer geen overengineerde oplossingen.
- Houd implementaties simpel en expliciet.
- Voeg alleen abstraheringen toe als daar een duidelijke reden voor is, zoals platformverschil of echt hergebruik.
- Respecteer de bestaande mapstructuur en naamgeving van dit project.

## Werken met bestaande code

- **Wijzig nooit zomaar bestaande werkende code zonder expliciete toestemming van de gebruiker.** Voorkomen dat functionaliteit onbedoeld wijzigt of stopt met werken.
- Stel wijzigingen altijd eerst voor voordat je bestaande code aanpast.
- Als bestaande code moet worden aangepast, leg dan duidelijk uit:
  - Wat er gewijzigd wordt
  - Waarom de wijziging nodig is
  - Welke impact dit heeft op functionaliteit
- Bij het toevoegen van nieuwe functionaliteit: bouw erop voort zonder bestaande werkende code te breken.
- Ga er altijd van uit dat bestaande code functioneel is, tenzij expliciet anders aangegeven.
- Vraag bij twijfel eerst om verduidelijking voordat je bestaande code wijzigt.

## C# stijl

- Gebruik file-scoped namespaces.
- Gebruik nullable reference types.
- Gebruik constructor injection.
- Gebruik `async`/`await` waar logisch.
- Gebruik nooit `.Result` of `.Wait()` op asynchrone code.
- Gebruik `var` alleen als het type direct duidelijk is uit de rechterkant.
- Gebruik duidelijke, voluit geschreven namen.
- Vermijd onnodige afkortingen.
- Houd classes en methoden klein en gericht op één verantwoordelijkheid.
- Gebruik één public class per file.

## Architectuur

- Houd de architectuur simpel.
- Gebruik geen extra lagen of patterns tenzij daar een duidelijke noodzaak voor is.
- **In de huidige refactorstappen: voeg nog geen repository pattern toe.** Repositories zijn een mogelijke latere architectuurstap, maar maken nu geen deel uit van de gewenste tussenstructuur.
- Gebruik de bestaande projectstructuur zoals die al in de repository staat.
- Gebruik bestaande mappen zoals `Data`, `Domain`, `Services`, `Hubs`, `DTOs` en `Pages`.
- Voeg geen nieuwe architectuurlagen toe zoals `Application`, `Infrastructure` of `Core` tenzij daar expliciet om gevraagd wordt.

### Gewenste tussenstructuur (nu implementeren)

**Blazor pagina's:**
- Blazor pagina's gebruiken NOOIT direct `AppDbContext`.
- Blazor pagina's werken uitsluitend via services en interfaces.
- Blazor pagina's werken met DTO's, niet met entities.

**Services:**
- Services worden gedefinieerd via interfaces (bijv. `IStudentService`).
- Services bevatten alle businesslogica, queries en updates.
- Services gebruiken in deze fase nog direct `AppDbContext` en EF Core.
- Services doen mapping tussen entities en DTO's.
- Services retourneren DTO's naar de UI.

**DTO's (Data Transfer Objects):**
- Gebruik DTO's voor communicatie tussen UI en services.
- DTO's zijn simpel: enkel properties zonder logica.
- DTO's worden gebruikt in plaats van entities in Blazor componenten.

**Data:**
- `AppDbContext` wordt alleen gebruikt door services, niet door Blazor pagina's.
- Entities blijven in de `Domain` map.

### Refactoring aanpak

- Refactor per functioneel gebied in kleine stappen.
- Begin met één pagina/functionaliteit tegelijk.
- Test na elke stap dat functionaliteit blijft werken.
- Houd bestaande werkende code intact tenzij expliciet aan refactoring wordt gewerkt.

## Blazor

- Gebruik Blazor Server.
- Houd code in `.razor` bestanden in `@code`.
- Maak geen losse `.cs` code-behind bestanden voor componenten.
- Houd UI-state in de component.
- **Blazor componenten gebruiken NOOIT direct `AppDbContext` of EF Core entities.**
- Blazor componenten werken uitsluitend via service-interfaces (bijv. `IStudentService`).
- Blazor componenten werken met DTO's, niet met entities.
- Plaats geen businesslogica in de Blazor component.
- Verplaats businesslogica naar services.
- Houd markup overzichtelijk en leesbaar.
- Voeg geen complexe componentabstracties toe zonder duidelijke reden.

## EF Core

- Gebruik EF Core met SQLite als uitgangspunt.
- Houd entities eenvoudig en expliciet.
- Gebruik duidelijke relaties en properties.
- **`AppDbContext` wordt alleen gebruikt in services, NOOIT in Blazor pagina's.**
- Services gebruiken voorlopig direct `AppDbContext` via constructor injection.
- **Repositories:** Een mogelijke latere architectuurstap, maar voeg ze in de huidige refactoring nog niet toe.
- Bedenk niet zelfstandig migrations tenzij daar expliciet om gevraagd wordt.
- Pas eerst entities en `DbContext` aan en stel daarna migrations voor.
- Vermijd onnodige complexiteit in modelconfiguratie.

## Services en interfaces

- Gebruik services voor businesslogica en infrastructuurlogica.
- **Definieer services altijd via interfaces** (bijv. `IStudentService` met implementatie `StudentService`).
- Services bevatten alle businesslogica, queries, updates en validaties.
- Services gebruiken `AppDbContext` direct via constructor injection.
- **Services doen mapping tussen entities en DTO's.**
- Services retourneren DTO's naar de UI, niet entities.
- Interfaces en services moeten Nederlandse XML-documentatie bevatten.
- Voeg comments toe waar nodig om niet-triviale logica te begrijpen.
- Vermijd overbodige comments.
- Documenteer vooral netwerkgedrag, Raspberry Pi-specifiek gedrag, en technische keuzes die niet direct duidelijk zijn.

### DTO's

- Gebruik Data Transfer Objects (DTO's) voor communicatie tussen UI en services.
- DTO's zijn simpele classes met alleen properties.
- DTO's bevatten geen businesslogica.
- Plaats DTO's in een `DTOs` map.
- Geef DTO's duidelijke namen die hun doel weergeven (bijv. `StudentDto`, `TestSessionDto`).
- DTO's kunnen computed properties bevatten voor weergave (bijv. `IsActive`).

## Logging en foutafhandeling

- Gebruik `ILogger<T>`.
- Voeg geen try/catch-blokken toe tenzij daar een duidelijke reden voor is.
- Gebruik try/catch alleen als fouten gelogd of hersteld kunnen worden.
- Slik exceptions nooit stilzwijgend weg.
- Gebruik `Warning` of `Error` waar passend.
- Gebruik geen extra loggingframeworks tenzij expliciet gevraagd.

## Tests

- Genereer geen unit tests tenzij daar expliciet om gevraagd wordt.
- Voeg niet automatisch testprojecten, mocks of testbestanden toe.

## README en documentatie

- Schrijf README-bestanden en setup-documentatie in het Nederlands.
- Houd README's duidelijk en praktisch.
- Documenteer architectuur, infrastructuur, setup en deploymentstappen helder.
- Geef concrete instructies voor Raspberry Pi, netwerkconfiguratie, EF Core migrations en deployment wanneer relevant.

## Wat Copilot moet vermijden

- Geen enterprise-overengineering.
- Geen overmatig gebruik van interfaces, factories, wrappers of base classes.
- Geen businesslogica in Blazor markup.
- Geen comments die letterlijk beschrijven wat de code al duidelijk laat zien.
- Geen automatische testgeneratie.
- Geen nieuwe projectstructuur verzinnen als de bestaande structuur voldoende is.
- Geen code voorstellen die afwijkt van de afgesproken stijl zonder duidelijke reden.
- **Geen ongeautoriseerde wijzigingen aan bestaande werkende code.**

## Voorkeursstijl bij nieuwe code

Bij nieuwe codevoorstellen:

1. sluit aan op bestaande bestanden en naamgeving
2. kies de simpelste onderhoudbare oplossing
3. houd businesslogica uit UI-bestanden
4. documenteer interfaces en services in het Nederlands met XML-docs
5. voeg alleen comments toe waar logica anders lastig te begrijpen is
6. houd code geschikt voor zowel lokale ontwikkeling op Windows als uitvoering op Raspberry Pi

## Specifieke context voor deze repository

Deze repository werkt met onder andere:

- Blazor Server
- SignalR
- EF Core
- SQLite
- Raspberry Pi / Linux-specifieke services
- netwerkmonitoring
- registratie- en dashboardschermen

Bij wijzigingen in deze onderdelen moet Copilot extra letten op:

- duidelijke scheiding tussen UI-state en businesslogica
- veilige omgang met achtergrondservices
- leesbare en expliciete netwerk- en platformlogica
- minimale impact op bestaande structuur

