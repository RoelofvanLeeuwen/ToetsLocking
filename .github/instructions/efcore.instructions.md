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
- Gebruik EF Core direct.
- Voeg geen repository pattern toe.
- Voeg geen Unit of Work abstrahering toe.
- Houd queries leesbaar en zo eenvoudig mogelijk.

## Migraties
- Bedenk niet zelfstandig migrations tenzij daar expliciet om gevraagd wordt.
- Pas eerst models en `DbContext` aan.
- Stel daarna pas een migration voor.

## Query-stijl
- Schrijf duidelijke LINQ queries.
- Kies leesbaarheid boven slimme compactheid.
- Gebruik `Include` alleen als nodig.
- Vermijd onnodig complexe query chains.

## Foutafhandeling en logging
- Voeg geen try/catch toe rond elke query.
- Log alleen waar dat echt zinvol is.
- Licht niet-triviale databasekeuzes toe met een korte Nederlandse comment.

## Documentatie
- Voor interfaces en services die EF Core gebruiken: voeg Nederlandse XML-documentatie toe.
- Beschrijf alleen wat functioneel relevant is.

## Wat vermijden
- Geen repositorylaag.
- Geen generieke base repositories.
- Geen complexe mapping-constructies zonder noodzaak.
- Geen verborgen side effects in save-logica.
