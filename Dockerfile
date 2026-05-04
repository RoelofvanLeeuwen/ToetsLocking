# =============================================================================
# Stage 1: Build
# Bouw de applicatie met de volledige .NET 8 SDK
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopieer het projectbestand en herstel dependencies (cache-efficiënt)
COPY ToetsLockingWifiMonitoring.Web/ToetsLockingWifiMonitoring.Web.csproj ToetsLockingWifiMonitoring.Web/
RUN dotnet restore ToetsLockingWifiMonitoring.Web/ToetsLockingWifiMonitoring.Web.csproj

# Kopieer de rest van de broncode en publiceer
COPY ToetsLockingWifiMonitoring.Web/ ToetsLockingWifiMonitoring.Web/
WORKDIR /src/ToetsLockingWifiMonitoring.Web
RUN dotnet publish ToetsLockingWifiMonitoring.Web.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# =============================================================================
# Stage 2: Runtime
# Gebruik de kleinere ASP.NET runtime image (ondersteunt ARM64 voor Raspberry Pi)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Installeer Linux-tools die nodig zijn voor netwerkmonitoring:
# - iw: uitlezen van wifi-stations (iw dev wlan0 station dump)
# - iproute2: ip/arp-commando's voor MAC-adres resolutie
RUN apt-get update && apt-get install -y --no-install-recommends \
    iw \
    iproute2 \
    && rm -rf /var/lib/apt/lists/*

# Map voor de SQLite-database (wordt gemount als volume vanuit docker-compose)
RUN mkdir -p /data

# Kopieer de gepubliceerde bestanden vanuit de build stage
COPY --from=build /app/publish .

# Standaard HTTP-poort (geen HTTPS op de Pi)
EXPOSE 5000

ENTRYPOINT ["dotnet", "ToetsLockingWifiMonitoring.Web.dll"]
