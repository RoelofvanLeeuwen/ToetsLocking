# User Story: Docker image publiceren via GitHub Actions bij versietag

**Branch:** `feature/docker-publish-ghcr`

## Story

**Als** ontwikkelaar  
**Wil ik** dat er automatisch een Docker image wordt gebouwd en gepubliceerd op `ghcr.io` wanneer ik een tag in het formaat `v1.0.0` op de `master`-branch zet  
**Zodat** ik de image vanaf een andere server kan pullen zonder handmatig te hoeven bouwen of overdragen

## Acceptatiecriteria

1. **Trigger** — De workflow start uitsluitend wanneer een tag wordt gepusht in het formaat `vMAJOR.MINOR.PATCH` (bijv. `v1.0.0`, `v2.3.1`).

2. **Twee tags gepubliceerd** — De workflow publiceert de image onder twee tags tegelijk:
   - `ghcr.io/roelofvanleeuwen/gctoetslocking:latest`
   - `ghcr.io/roelofvanleeuwen/gctoetslocking:1.0.0` (versienummer zonder de `v`-prefix, afgeleid van de git-tag)

3. **Platform** — De image wordt uitsluitend gebouwd voor `linux/arm64` (Raspberry Pi).

4. **Dockerfile** — De build gebruikt de `Dockerfile` in de root van de repository.

5. **Authenticatie bij publicatie** — De workflow authenticeert bij `ghcr.io` met `GITHUB_TOKEN`; er zijn geen externe secrets nodig.

6. **Zichtbaarheid** — De gepubliceerde image is privaat. Ophalen vereist authenticatie met een geldig GitHub PAT met scope `read:packages`.

7. **Geen pre-build checks** — Er worden geen extra checks (tests, linting) uitgevoerd voor de Docker build; `main` bevat altijd geteste code.

8. **Traceerbaarheid** — De gepubliceerde image bevat OCI-labels:
   - `org.opencontainers.image.source` — URL naar de GitHub-repository
   - `org.opencontainers.image.version` — versienummer afgeleid van de git-tag
   - `org.opencontainers.image.revision` — git commit SHA

9. **Build cache** — De workflow maakt gebruik van GitHub Actions cache (`type=gha`) om herhaalde builds te versnellen.

## Implementatie

- Workflow: `.github/workflows/docker-publish.yml`
- Gebruikt: `docker/setup-qemu-action`, `docker/setup-buildx-action`, `docker/login-action`, `docker/metadata-action`, `docker/build-push-action`
