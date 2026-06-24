# Branch: feature/ip-info-en-powershell-helper

Dit document beschrijft de doelen van deze branch. Alle drie de onderdelen hieronder moeten afgerond zijn voordat deze branch gemerged mag worden naar `master`.

## Doel 1 — IP-adressen uitbreiden met Ethernet-kaart

De applicatie toont momenteel het IP-adres van de WiFi-adapter. Dit moet uitgebreid worden zodat ook het IP-adres van de Ethernet-kaart getoond wordt.

**Waar te tonen:** op een docentpagina waar netwerkinformatie van de Pi zichtbaar is (huidig: `Status.razor` of een nieuw te bepalen plek).

**Wat er moet werken:**
- Het IP-adres van de WiFi-adapter wordt al getoond; dit blijft intact.
- Naast het WiFi-IP wordt ook het IP-adres van de Ethernet-interface (`eth0` of configureerbaar) opgehaald en getoond.
- Op Linux wordt het IP-adres uitgelezen via `ip addr` of een vergelijkbaar systeemcommando.
- Op Windows (development) wordt een lege of mock-waarde getoond.
- Het IP-adres van de Ethernet-kaart wordt via de service-laag aangeboden, niet direct vanuit de pagina.

**Configuratie:**
- De Ethernet-interface is configureerbaar via `appsettings.json`, vergelijkbaar met `Monitoring:Interface`.
- Verwachte sleutel: `Network:EthernetInterface` (of vergelijkbare naam, te bepalen bij implementatie).

---

## Doel 2 — PowerShell-script ter download voor docenten

Er komt een PowerShell-script beschikbaar als download via de webapplicatie. Dit script is bedoeld voor docenten om hun laptop of werkstation te configureren voor gebruik tijdens een toets op het Pi-netwerk.

**Wat er moet werken:**
- Het script is opgeslagen als statisch bestand in de applicatie (bijv. `wwwroot/downloads/`).
- De applicatie biedt een downloadlink aan op een docentpagina.
- Het script is downloadbaar via een gewone HTTP-aanvraag, zonder authenticatie.
- De downloadnaam is duidelijk en herkenbaar voor een docent.

**Inhoud van het script (te bepalen bij implementatie):**
- Het script helpt de docent om verbinding te maken met het Pi-netwerk of netwerkinstellingen te controleren.
- Exacte inhoud wordt bepaald op basis van de praktijksituatie (IP-bereik, DNS, routes).

---

## Doel 3 — Uitlegpagina voor het PowerShell-script

Er komt een Blazor-pagina in de applicatie die uitlegt hoe het PowerShell-script gebruikt moet worden.

**Wat er moet werken:**
- De pagina is bereikbaar voor ingelogde docenten (via de bestaande docentafscherming).
- De pagina bevat:
  - een korte uitleg van wat het script doet
  - stap-voor-stap instructies voor het uitvoeren van het script op Windows
  - de downloadknop of -link naar het script (zie Doel 2)
  - relevante netwerkinformatie van de Pi (IP-adressen uit Doel 1)
- De pagina past binnen de bestaande Blazor-architectuur: service-laag voor data, DTO's voor weergave, geen directe `AppDbContext` in de pagina.

---

## Definition of Done voor deze branch

- [ ] IP-adres van de Ethernet-kaart is zichtbaar in de applicatie naast het WiFi-IP
- [ ] PowerShell-script is downloadbaar via de webapplicatie
- [ ] Uitlegpagina is beschikbaar voor docenten met instructies en downloadknop
- [ ] Alle drie onderdelen werken op de Raspberry Pi in productie
- [ ] Development-modus (Windows) crasht niet door ontbrekende Linux-netwerkinformatie
- [ ] Geen bestaande functionaliteit is gebroken
