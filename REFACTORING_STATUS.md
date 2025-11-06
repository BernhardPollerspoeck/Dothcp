# Orleans zu SQLite Refactoring - Status

## Abgeschlossen ✅

### Infrastructure
- ✅ SQLite Entity Framework Core Pakete hinzugefügt
- ✅ Domain Models erstellt (in `Models/` Ordner)
  - DhcpLease
  - DhcpReservation
  - IpAddressStatus
  - AppSetting
  - ClientInfo
- ✅ DbContext erstellt (`Data/DhcpDbContext.cs`)
- ✅ Alle Repositories erstellt:
  - LeaseRepository
  - ReservationRepository
  - IpAddressRepository
  - SettingsRepository
  - ClientRepository

### Core Services (Ersatz für Grains)
- ✅ LeaseService (ersetzt DhcpLeaseGrain)
- ✅ ReservationServiceCore (ersetzt DhcpReservationGrain + Manager)
- ✅ IpAddressService (ersetzt IpAddressInformationGrain)
- ✅ ConfigurationService (ersetzt SettingsGrain)

### Services aktualisiert
- ✅ Program.cs - SQLite initialisiert, Repositories + Core Services registriert
- ✅ ReservationService - verwendet jetzt ReservationServiceCore
- ✅ SettingsService - verwendet jetzt ConfigurationService
- ✅ SettingsLoaderService - verwendet jetzt ConfigurationService
- ✅ FirstRunService - verwendet jetzt ConfigurationService

### Dependencies
- ✅ Orleans Pakete aus .csproj entfernt
- ✅ EF Core SQLite Pakete hinzugefügt

## Services aktualisiert (Teil 2)
- ✅ DashboardService - verwendet jetzt ILeaseService statt IGrainFactory
- ✅ LeaseGrainSearchService - verwendet jetzt ILeaseService für Lease-Suche
- ✅ OfferGeneratorService - alle Orleans-Abhängigkeiten entfernt
- ✅ NetworkListener - verwendet jetzt DhcpMessageHandler statt Grains
- ✅ DhcpMessageHandler (NEU) - ersetzt DhcpManagerGrain mit vollständiger DHCP-Logik

## Razor Components aktualisiert
- ✅ Settings.razor - verwendet jetzt IConfigurationService statt IClusterClient
- ✅ Leases.razor - verwendet jetzt ILeaseService statt IGrainFactory
- ✅ FirstTimeSetup.razor - verwendet jetzt IConfigurationService statt IClusterClient

## Grain-Dateien entfernt
- ✅ Grains/DhcpManager/* - alle Grains entfernt (DhcpLeaseGrain, DhcpManagerGrain, DhcpReservationGrain, etc.)
- ✅ Grains/IpAddress/* - alle Grains entfernt
- ✅ Grains/Settings/* - alle Grains entfernt
- ✅ Grains/MessageParser/* - MessageParserGrain entfernt
- ✅ FileStorage/* - komplett gelöscht (Orleans-spezifisch)

## Noch zu tun ⚠️

### Tests
- Tests von xUnit auf MSTest portieren
- Neue Tests für alle Core Services schreiben
- Bestehende Tests anpassen (ReservationServiceTests, etc.)

### Weitere Aufgaben
- End-to-End Testing durchführen
- Sicherstellen dass alle DHCP-Funktionen korrekt funktionieren

## Nächste Schritte

1. ✅ ~~DashboardService aktualisieren~~
2. ✅ ~~LeaseGrainSearchService aktualisieren~~
3. ✅ ~~OfferGeneratorService aktualisieren~~
4. ✅ ~~NetworkListener aktualisieren~~
5. ✅ ~~Grain-Dateien löschen~~
6. ✅ ~~FileStorage-Ordner löschen~~
7. ✅ ~~Razor Components aktualisieren~~
8. Tests auf MSTest portieren
9. End-to-End Testing durchführen

## Architektur-Änderungen

### Vorher (mit Orleans):
```
NetworkListener -> IGrainFactory -> Grains (DhcpManagerGrain, etc.)
                                    -> IPersistentState -> File Storage
```

### Nachher (mit SQLite):
```
NetworkListener -> Services (LeaseService, etc.)
                   -> Repositories
                      -> DbContext
                         -> SQLite Database
```

## Datenpersistierung

### Vorher:
- Orleans FileGrainStorage in `Orleans/GrainState/v1/`
- JSON-basierte Serialisierung

### Nachher:
- SQLite Datenbank in `Data/dhcp.db`
- Entity Framework Core

