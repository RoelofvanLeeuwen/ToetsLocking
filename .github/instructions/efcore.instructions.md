---
applyTo: "src/**/*.cs"
---

# EF Core instructies

Gebruik EF Core met SQLite op een eenvoudige en expliciete manier.

## Modellen en DbContext
- Houd entities eenvoudig en goed leesbaar.
- Gebruik duidelijke namen voor tabellen, relaties en properties.
- Configureer alleen expliciet wat nodig is.
- Gebruik nullable reference types correct.

## Toegang tot data
- **`AppDbContext` wordt alleen gebruikt in services, NOOIT in Blazor pagina's.**
- Services gebruiken `AppDbContext` direct via constructor injection.
- Services doen mapping tussen entities en DTO's.
- Blazor pagina's werken alleen met services en DTO's, niet met entities.
- **Repositories:** Mogelijke latere architectuurstap; voeg ze in de huidige refactoring voorlopig nog niet toe.
- Voeg geen Unit of Work abstrahering toe.

## Services en businesslogica
- Services bevatten alle businesslogica, queries en updates.
- Definieer services via interfaces (bijv. `IStudentService`).
- Services retourneren DTO's naar de UI, niet entities.
- Houd queries in services leesbaar en expliciet.

## Migraties
- Bedenk niet zelfstandig migrations tenzij daar expliciet om gevraagd wordt.
- Pas eerst models en `DbContext` aan.
- Stel daarna pas een migration voor.

## Query-stijl
- Schrijf duidelijke LINQ queries in services.
- Kies leesbaarheid boven slimme compactheid.
- Gebruik `Include` alleen als nodig.
- Vermijd onnodig complexe query chains.
- Maak mapping van entities naar DTO's expliciet en leesbaar.

## Foutafhandeling en logging
- Voeg geen try/catch toe rond elke query.
- Log alleen waar dat echt zinvol is.
- Licht niet-triviale databasekeuzes toe met een korte Nederlandse comment.

## Documentatie
- Voor interfaces en services die EF Core gebruiken: voeg Nederlandse XML-documentatie toe.
- Beschrijf alleen wat functioneel relevant is.

## Wat vermijden
- Geen direct gebruik van `AppDbContext` in Blazor pagina's.
- Geen entities teruggeven naar de UI - gebruik DTO's.
- Geen repositorylaag toevoegen in de huidige refactoring (mogelijke latere stap).
- Geen generieke base repositories.
- Geen complexe mapping-constructies zonder noodzaak.
- Geen verborgen side effects in save-logica.

