# ADR-0001: Development fallback voor MAC-resolutie op infrastructuurniveau

## Status

**Geaccepteerd**  
Oorspronkelijk ingevoerd in 2024, nog steeds actueel op **6 mei 2026**.

## Context

Studentregistratie en studentstatus zijn afhankelijk van het kunnen herleiden van een MAC-adres uit het client-IP.

In productie is dat nodig voor:

- registratie via `/register`
- statusbepaling via `/myscreen`
- koppeling tussen netwerkstation en student

Tijdens lokale ontwikkeling werkt die resolutie niet betrouwbaar voor `localhost`, `127.0.0.1` en `::1`, omdat loopbackinterfaces geen bruikbare ARP/neighbor entry opleveren.

Zonder fallback zou dit blokkeren:

- functioneel testen van studentregistratie
- testen van `MyScreen`
- end-to-end flowtests zonder echte WiFi clients

## Beslissing

We houden MAC-resolutie in de infrastructuurlaag en gebruiken een decorator:

- `WindowsMacResolver` of `LinuxMacResolver` doet de echte lookup
- `DevelopmentMacResolverDecorator` voegt optioneel een development fallback toe

De decorator wordt via dependency injection als `IMacResolver` geregistreerd.

## Implementatie

### Inner resolvers

- `WindowsMacResolver` gebruikt `arp -a <ip>`
- `LinuxMacResolver` gebruikt `/usr/sbin/ip neigh show <ip>`

### Decoratorgedrag

De decorator werkt als volgt:

1. probeer eerst de inner resolver
2. als een MAC gevonden is: direct teruggeven
3. als geen MAC gevonden is:
   - buiten `Development`: `null`
   - in `Development`: optionele mock MAC uit configuratie

Gebruikte configuratie:

```json
{
  "DevelopmentTesting": {
    "EnableMockMacAddress": true,
    "MockMacAddress": "aa:bb:cc:dd:ee:ff"
  }
}
```

## Waarom deze keuze

### 1. UI blijft schoon

De fallbacklogica hoort niet thuis in `Register.razor`. Die pagina moet niet weten:

- of de app in development draait
- welke configuratieflag de fallback activeert
- hoe platformspecifieke MAC-resolutie werkt

### 2. Hergebruik

Dezelfde `IMacResolver` wordt gebruikt door meerdere flows:

- registratie
- studentstatus

De fallback zit dus op één plek.

### 3. Productieveiligheid

De decorator gebruikt alleen mockdata als:

- `IWebHostEnvironment.IsDevelopment()` waar is
- `DevelopmentTesting:EnableMockMacAddress` aan staat
- `MockMacAddress` is ingevuld

Daardoor wordt mockgedrag niet per ongeluk in productie actief.

### 4. Testbaarheid

De resolveerlogica is los testbaar zonder UI-componenten te hoeven mocken.

## Gevolgen

### Positief

- development kan zonder echt netwerkpad doorgaan
- registratieflow blijft servicegericht
- UI bevat geen environment- of configlogica
- één implementatiepunt voor alle MAC-fallbacks

### Negatief

- extra abstractielaag
- debuggingstack is iets langer
- configuratie moet correct gezet worden in development

## Alternatieven die bewust niet zijn gekozen

### Fallback in `Register.razor`

Afgewezen omdat dit infrastructuurkennis in de UI duwt en bij andere pagina's opnieuw gedupliceerd zou worden.

### Altijd een aparte `MockMacResolver` registreren in development

Afgewezen omdat we in development nog steeds eerst echte resolutie willen proberen. De gewenste strategie is:

- eerst echt
- daarna fallback

### Middleware of `HttpContext.Items`

Afgewezen omdat dat de intentie verbergt en geen nette interface biedt.

## Huidige nuance in de codebasis

`MyScreen.razor` heeft daarnaast nog een **extra** development-only querystring fallback `?mac=...`.

Die vervangt de decorator niet. De twee mechanismen hebben verschillende doelen:

- decorator: generieke infrastructuurfallback voor MAC-resolutie uit IP
- querystring fallback: ontwikkelgemak voor directe studentstatussimulatie

## Relatie met andere infrastructuurkeuzes

Deze ADR staat naast de runtimekeuze voor `IStationProvider`:

- Windows -> `MockStationProvider`
- Linux -> `LinuxIwStationProvider`

Samen maken die keuzes lokale end-to-end tests mogelijk zonder Raspberry Pi.

## Conclusie

De development fallback blijft een infrastructuurverantwoordelijkheid. De huidige decoratoropzet is nog steeds de juiste keuze omdat die:

- productiegedrag schoon houdt
- development bruikbaar maakt
- UI-complexiteit beperkt
- meerdere flows bedient met dezelfde abstrahering
