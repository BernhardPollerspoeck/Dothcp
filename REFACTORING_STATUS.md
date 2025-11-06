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

## Noch zu tun ⚠️

### Services die noch Orleans-Code enthalten
1. **DashboardService** - verwendet IGrainFactory und ILeaseGrainSearchService
2. **LeaseGrainSearchService** - verwendet IGrainFactory für Lease-Suche
3. **OfferGeneratorService** - verwendet IGrainFactory für DhcpLeaseGrain, IpAddressInformationGrain, etc.
4. **NetworkListener** - verwendet IGrainFactory für MessageParserGrain und DhcpManagerGrain

### Grain-Dateien
- Grains/DhcpManager/* - sollten entweder entfernt oder zu normalen Services konvertiert werden
- Grains/IpAddress/* - sollten entfernt werden
- Grains/Settings/* - sollten entfernt werden
- Grains/MessageParser/* - sollte entfernt oder konvertiert werden
- FileStorage/* - kann komplett gelöscht werden (Orleans-spezifisch)

### Tests
- Tests von xUnit auf MSTest portieren
- Neue Tests für alle Core Services schreiben
- Bestehende Tests anpassen (ReservationServiceTests, etc.)

## Nächste Schritte

1. DashboardService aktualisieren
2. LeaseGrainSearchService aktualisieren
3. OfferGeneratorService aktualisieren (komplex!)
4. NetworkListener aktualisieren (komplex!)
5. Grain-Dateien löschen
6. FileStorage-Ordner löschen
7. Tests auf MSTest portieren
8. Testen und Bugs fixen

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

