# StudentWifiMonitoring – stap-voor-stap deployment naar Raspberry Pi met Docker

## Doel

Deze handleiding beschrijft **exact** wat je vanaf nu moet doen om `StudentWifiMonitoring` op een Raspberry Pi te draaien in Docker.

Deze handleiding gaat uit van de situatie die nu al bereikt is:

- de applicatie draait lokaal in Docker op Windows
- er is een aparte Pi-compose: `docker-compose.pi.yml`
- de app draait op de Pi **zonder HTTPS**
- de monitoringinterface op de Pi is **`eth1`**
- SQLite moet persistent opgeslagen worden
- de app gebruikt Linux tools zoals `iw` en `ip`
- de app luistert in Docker op **poort 5000**

---

# 1. Wat je nu hebt

Je werkt met deze Docker-bestanden:

- `Dockerfile`
- `.dockerignore`
- `docker-compose.local.yml` → alleen voor lokaal testen op Windows
- `docker-compose.pi.yml` → voor Raspberry Pi productie
- `docker-compose.yml` → oud bestand, voorlopig niet gebruiken

**Belangrijk:**  
Gebruik voortaan:

- lokaal op Windows: `docker-compose.local.yml`
- op de Raspberry Pi: `docker-compose.pi.yml`

---

# 2. Wat je op de Raspberry Pi nodig hebt

Op de Raspberry Pi moet aanwezig zijn:

- Raspberry Pi OS
- netwerkwerkende Pi
- Docker
- Docker Compose plugin

Controleer op de Pi:

```bash
docker --version
docker compose version
```

Als beide werken, kun je door.

---

# 3. Aanbevolen mapstructuur op de Pi

Gebruik op de Pi een vaste map:

```bash
/opt/studentwifimonitoring
```

En daarbinnen een data-map:

```bash
/opt/studentwifimonitoring/data
```

Maak deze aan met:

```bash
sudo mkdir -p /opt/studentwifimonitoring/data
sudo chown -R $USER:$USER /opt/studentwifimonitoring
```

---

# 4. Welke bestanden je naar de Pi moet kopiëren

Je hebt op de Pi minimaal nodig:

- de volledige broncode van het project
- `Dockerfile`
- `.dockerignore`
- `docker-compose.pi.yml`

De eenvoudigste manier is om de hele projectmap naar de Pi te kopiëren.

## Optie A – kopiëren met SCP vanaf Windows PowerShell

Ga op je Windows-machine naar de root van je project en kopieer alles naar de Pi:

```powershell
scp -r . pi@<ip-van-je-pi>:/opt/studentwifimonitoring
```

> Vervang `pi` door jouw Linux-gebruiker als je een andere gebruikt.

## Optie B – via WinSCP

Je kunt ook WinSCP gebruiken en de projectmap uploaden naar:

```text
/opt/studentwifimonitoring
```

---

# 5. Controleer op de Pi of alles op de juiste plek staat

Log in op de Pi:

```bash
ssh pi@<ip-van-je-pi>
```

Ga naar de map:

```bash
cd /opt/studentwifimonitoring
```

Controleer of je minimaal ziet:

```bash
ls
```

Je wilt daar in elk geval zien:

- `Dockerfile`
- `docker-compose.pi.yml`
- projectmappen met de broncode
- solution/projectbestanden

---

# 6. Pas eerst de docent-pincode aan

Open `docker-compose.pi.yml` op de Pi:

```bash
nano docker-compose.pi.yml
```

Zoek deze regel:

```yaml
Teacher__Password=vervang-dit
```

Vervang die door je echte pincode, bijvoorbeeld:

```yaml
Teacher__Password=7382
```

Gebruik niet `1234` in productie.

Sla daarna op:

- `Ctrl + O`
- `Enter`
- `Ctrl + X`

---

# 7. Controleer de belangrijkste productie-instellingen

In `docker-compose.pi.yml` moeten deze dingen kloppen:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:5000`
- `ConnectionStrings__Default=Data Source=/data/app.db`
- `Monitoring__Interface=eth1`
- `Monitoring__PollSeconds=2`
- `Teacher__Password=<jouw pincode>`
- `ForceHttps=false`

Daarnaast moet er een volume zijn voor de database, zodat data bewaard blijft.

---

# 8. Bouw de container op de Pi

Ga op de Pi naar de projectmap:

```bash
cd /opt/studentwifimonitoring
```

Bouw daarna de image:

```bash
docker compose -f docker-compose.pi.yml build --no-cache
```

## Wat je mag verwachten

De eerste build duurt vaak wat langer, omdat:

- base images worden opgehaald
- .NET build/publish wordt uitgevoerd
- Linux packages zoals `iw` en `iproute2` worden geïnstalleerd

## Als de build fout gaat

Controleer dan eerst:

1. sta je in de juiste map?
2. is de volledige broncode meegekopieerd?
3. is er genoeg vrije ruimte op de Pi?
4. heb je internet op de Pi?

---

# 9. Start de container op de Pi

Als de build lukt:

```bash
docker compose -f docker-compose.pi.yml up -d
```

Controleer daarna of de container draait:

```bash
docker compose -f docker-compose.pi.yml ps
```

Controleer logs:

```bash
docker compose -f docker-compose.pi.yml logs -f
```

---

# 10. Waar je in de logs op moet letten

Je wilt in de logs ongeveer dit gedrag zien:

- applicatie start zonder exception
- environment is `Production`
- database migratie wordt uitgevoerd of de database wordt aangemaakt
- de app luistert op poort 5000
- geen crash direct na startup

Let vooral op regels als:

- `Now listening on: http://...:5000`
- startup logging van ASP.NET Core
- eventuele foutmeldingen rond SQLite
- eventuele foutmeldingen rond `iw` of `ip`

---

# 11. Browsertest op de Pi

Open op een ander apparaat in hetzelfde netwerk de browser en ga naar:

```text
http://<ip-van-je-pi>:5000
```

Bijvoorbeeld:

```text
http://192.168.1.50:5000
```

## Wat je nu wilt testen

### Studenttest
- opent de app?
- kun je `Register` openen?
- kun je `MyScreen` openen?
- zie je als student géén docentinhoud?

### Docenttest
- ga handmatig naar de verborgen docentloginroute
- log in met de ingestelde pincode
- controleer dat docentpagina’s werken

---

# 12. Controleer of SQLite persistent is

De app gebruikt SQLite op:

```text
/data/app.db
```

Controleer of de database is aangemaakt:

```bash
docker volume ls
```

Zoek het volume op dat door compose is aangemaakt.

Je kunt ook de container inspecteren:

```bash
docker compose -f docker-compose.pi.yml exec <servicenaam> sh
```

En daarbinnen kijken:

```sh
ls /data
```

Je wilt zien:

- `app.db`

> Let op: de servicenaam in compose kan nog een oude naam hebben. Gebruik `docker compose ... ps` om te zien hoe de service heet.

---

# 13. Test of data blijft bestaan na restart

Dit is belangrijk.

## Eerst wat data aanmaken
Bijvoorbeeld:

- log in als docent
- maak een toets aan
- of registreer een student

## Daarna container herstarten

```bash
docker compose -f docker-compose.pi.yml down
docker compose -f docker-compose.pi.yml up -d
```

Open daarna opnieuw de app.

### Verwachting
De eerder aangemaakte data moet er nog zijn.

Als data weg is, dan klopt de volume-koppeling nog niet goed.

---

# 14. Testen van monitoring op de Pi

Pas **na** de algemene browsertest en persistentietest doe je dit.

De open technische vraag was namelijk of deze Linux-commando’s goed werken **in de container**:

- `iw`
- `ip neigh`

## Test 1 – werkt `iw` in de container?

Open een shell in de container:

```bash
docker compose -f docker-compose.pi.yml exec <servicenaam> sh
```

Voer uit:

```sh
iw dev
```

En daarna bijvoorbeeld:

```sh
iw dev eth1 station dump
```

> Mogelijk is `eth1` geen wifi-device voor `iw`. Dat hangt af van hoe de Pi netwerktechnisch is ingericht. Als `iw` een andere interface verwacht, moet je dat hier zien.

## Test 2 – werkt `ip neigh` in de container?

Nog steeds in de container:

```sh
ip neigh
```

### Wat je hiermee wilt bevestigen
- ziet de container de host-netwerkgegevens?
- zijn de benodigde Linux tools bruikbaar?
- kan monitoring straks echt functioneren?

---

# 15. Als monitoring niet werkt: wat dan controleren

Controleer dan deze punten:

## A. Klopt `network_mode: host` echt?
Dat is nodig zodat de container het host-netwerk kan zien.

## B. Bestaat `eth1` echt op de Pi?
Controleer op de Pi zelf:

```bash
ip addr
```

## C. Is `eth1` echt de interface waar de studenten op binnenkomen?
Dat had je al aangegeven, maar dit kun je hier nog technisch bevestigen.

## D. Is `iw` het juiste hulpmiddel voor die interface?
Als `eth1` geen wireless station-interface is maar bijvoorbeeld een bridge of een andere netwerklaag, dan kan `iw` mogelijk niet de juiste tool zijn voor die interface. Dan moet je kijken of de echte wireless interface een andere naam heeft.

---

# 16. Belangrijk verschil tussen Windows-test en Pi-test

Lokaal op Windows testte je vooral:

- start de app?
- werkt de browser?
- werkt login?
- werkt database?

Op de Pi test je extra:

- klopt het netwerkgedrag?
- werken `iw` en `ip neigh`?
- ziet monitoring echte clients op `eth1`?

Dus de **Pi-test is de echte functionele eindtest**.

---

# 17. Handige Docker-commando’s op de Pi

## Containerstatus
```bash
docker compose -f docker-compose.pi.yml ps
```

## Logs volgen
```bash
docker compose -f docker-compose.pi.yml logs -f
```

## Container stoppen
```bash
docker compose -f docker-compose.pi.yml down
```

## Container opnieuw starten
```bash
docker compose -f docker-compose.pi.yml up -d
```

## Herbuilden
```bash
docker compose -f docker-compose.pi.yml build --no-cache
docker compose -f docker-compose.pi.yml up -d
```

## Shell in container
```bash
docker compose -f docker-compose.pi.yml exec <servicenaam> sh
```

---

# 18. Praktische eerste testvolgorde

Gebruik deze volgorde precies:

## Fase A – draait de app?
1. builden
2. starten
3. logs controleren
4. browser openen

## Fase B – werkt de basis?
5. docentlogin testen
6. studentregistratie testen
7. docent/student-afscherming testen

## Fase C – blijft data bestaan?
8. toets of student aanmaken
9. container restart
10. controleren of data nog bestaat

## Fase D – werkt monitoring?
11. `ip addr` op de Pi
12. `iw` in de container
13. `ip neigh` in de container
14. studentapparaat verbinden/verbreken
15. controleren of dashboard/status reageert

---

# 19. Wat nu nog géén doel is

Nog niet doen:

- HTTPS toevoegen
- reverse proxy toevoegen
- instellingenpagina bouwen
- Kubernetes of andere complexiteit
- meerdere containers/services optuigen

Voor release 1 is het doel simpel:

- één container
- SQLite persistent
- docent/student-flow werkt
- monitoring werkt op de Pi

---

# 20. Samenvatting van de eerstvolgende concrete acties

## Op de Pi
1. Docker controleren
2. `/opt/studentwifimonitoring` aanmaken
3. projectbestanden kopiëren
4. `docker-compose.pi.yml` aanpassen met echte docentpincode
5. `docker compose -f docker-compose.pi.yml build --no-cache`
6. `docker compose -f docker-compose.pi.yml up -d`
7. logs bekijken
8. browser openen op `http://<pi-ip>:5000`

## Daarna
9. databasepersistentie testen
10. monitoring testen met `iw` en `ip neigh`

---

# 21. Als je straks vastloopt

Als iets fout gaat, geef dan steeds precies deze drie dingen door:

1. **het commando** dat je hebt uitgevoerd
2. **de exacte foutmelding**
3. **in welke stap van deze handleiding** je zat

Dan is gericht helpen veel makkelijker.
