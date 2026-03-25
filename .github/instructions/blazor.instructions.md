---
applyTo: "src/**/*.razor,src/**/*.cshtml,src/**/Pages/**/*.cs"
---

# Blazor instructies

Gebruik in deze repository Blazor Server als uitgangspunt.

## Componentopbouw
- Houd componentlogica in het `.razor` bestand in `@code`.
- Maak geen aparte code-behind bestanden voor componenten.
- Houd markup overzichtelijk en functioneel.
- Houd UI-state in de component.
- Plaats geen businesslogica in de component.
- Verplaats businesslogica naar services.

## Structuur
- Gebruik duidelijke secties in componenten:
  - injecties bovenaan
  - markup
  - `@code` onderaan
- Houd componenten klein en leesbaar.
- Splits alleen componenten op als dat echt helpt voor hergebruik of leesbaarheid.

## Data en services
- Gebruik dependency injection voor services.
- Spreek services direct aan vanuit de component als dat eenvoudig en duidelijk blijft.
- Voeg geen extra state-management library toe tenzij expliciet gevraagd.

## SignalR en realtime gedrag
- Gebruik SignalR pragmatisch en leesbaar.
- Voeg alleen reconnect-logica toe als dat functioneel nodig is.
- Houd realtime code beperkt en begrijpelijk.
- Licht niet-triviale realtime logica toe met een korte Nederlandse comment.

## UI-richtlijnen
- UI-teksten mogen Nederlands zijn.
- Houd de UI functioneel en rustig.
- Gebruik duidelijke labels en foutmeldingen.
- Vermijd onnodig complexe styling of componentframeworks tenzij expliciet gevraagd.

## Wat vermijden
- Geen businesslogica in markup.
- Geen zware abstracties voor simpele componenten.
- Geen onnodige helper-methods als de code ook direct leesbaar is.
