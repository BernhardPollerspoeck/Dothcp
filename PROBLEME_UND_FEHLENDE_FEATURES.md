# Vollständige Problemanalyse - DHCP Server

Erstellt: 2025-11-06
Status nach Orleans-Entfernung

---

## 🔴 KRITISCHE PROBLEME (Sofort beheben!)

### 1. **Doppelte LeaseStatus Enums mit unterschiedlichen Werten**
**Schweregrad:** KRITISCH
**Dateien:**
- `/src/qt.qsp.dhcp.Server/Models/DhcpLease.cs:105-111` (4 Werte: Active, Renewed, Expired, Released)
- `/src/qt.qsp.dhcp.Server/Grains/DhcpManager/LeaseStatus.cs:3-8` (3 Werte: Active, Expired, Renewed)

**Problem:**
- Zwei verschiedene Enums mit unterschiedlichen Werten
- `Models.LeaseStatus` hat `Released`, `Grains.LeaseStatus` nicht
- Cast zwischen den beiden führt zu Fehlern
- Grains/DhcpManager-Ordner sollte bereits gelöscht sein, existiert aber noch

**Auswirkung:**
- InvalidCastException beim Konvertieren zwischen alten und neuen Models
- Lease-Status kann falsch interpretiert werden
- Datenbank-Status inkonsistent mit UI-Darstellung

**Lösung:**
- Grains/DhcpManager/LeaseStatus.cs komplett löschen
- Nur Models.LeaseStatus verwenden
- Alle Referenzen auf Grains.DhcpManager.LeaseStatus entfernen

---

### 2. **Doppelte IReservationService Interfaces**
**Schweregrad:** KRITISCH
**Dateien:**
- `/src/qt.qsp.dhcp.Server/Services/IReservationService.cs`
- `/src/qt.qsp.dhcp.Server/Services/Core/IReservationService.cs`

**Problem:**
- Zwei Interfaces mit gleichem Namen aber unterschiedlichen Methoden-Signaturen
- Führt zu Verwirrung und falscher Dependency Injection
- Program.cs registriert möglicherweise das falsche Interface

**Auswirkung:**
- Dependency Injection Resolution-Fehler zur Laufzeit
- Falsche Service-Implementierung wird injiziert

**Lösung:**
- Altes IReservationService umbenennen oder löschen
- Nur Core-Version verwenden
- Program.cs Service-Registrierung prüfen und korrigieren

---

### 3. **Race Condition bei IP-Vergabe**
**Schweregrad:** KRITISCH
**Datei:** `/src/qt.qsp.dhcp.Server/Services/DhcpMessageHandler.cs`

**Problem:**
- Keine Synchronisation bei IP-Status-Prüfung und -Vergabe
- Zwischen GetStatusAsync und SetStatusAsync kann eine andere Anfrage die IP belegen
- Zwei gleichzeitige DISCOVER-Anfragen können dieselbe IP bekommen

**Code-Stelle:** `HandleDiscoverAsync`, Zeilen ~100-150

**Auswirkung:**
- **IP-Konflikt im Netzwerk** - zwei Clients mit derselbe IP
- Schwer zu debuggen, tritt nur unter Last auf
- Netzwerk-Instabilität

**Lösung:**
```csharp
// Option 1: Lock-basiert
private static readonly SemaphoreSlim _ipAllocationLock = new SemaphoreSlim(1, 1);

await _ipAllocationLock.WaitAsync();
try {
    var status = await _ipAddressService.GetStatusAsync(ipAddress);
    if (status == EIpAddressStatus.Available) {
        await _ipAddressService.SetStatusAsync(ipAddress, EIpAddressStatus.Offered);
    }
} finally {
    _ipAllocationLock.Release();
}

// Option 2: Transaktional auf DB-Ebene
```

---

### 4. **Lease Renewal nicht korrekt implementiert**
**Schweregrad:** KRITISCH
**Datei:** `/src/qt.qsp.dhcp.Server/Services/DhcpMessageHandler.cs`

**Problem im HandleRequestAsync:**
- Bei DHCP REQUEST für Lease-Renewal wird immer eine neue Lease erstellt
- Bestehende Lease wird nicht erneuert
- LeaseStart wird immer auf DateTime.UtcNow gesetzt, selbst bei Renewal

**Code-Problem (Zeile ~200-250):**
```csharp
// FALSCH: Erstellt immer neue Lease
var lease = new DhcpLease
{
    MacAddress = clientId,
    IpAddressString = requestedIp,
    LeaseStart = DateTime.UtcNow,  // <-- Sollte alte LeaseStart behalten bei Renewal
    // ...
};
await _leaseService.UpdateLeaseAsync(lease);
```

**Auswirkung:**
- Clients verlieren ihre Lease-History
- Lease-Statistiken falsch
- Lease-Ablauf wird ständig zurückgesetzt

**Lösung:**
```csharp
// Prüfen ob Lease existiert
var existingLease = await _leaseService.GetLeaseAsync(requestedIp);
if (existingLease != null && existingLease.MacAddress == clientId)
{
    // RENEWAL: Lease-Zeit verlängern, aber LeaseStart beibehalten
    existingLease.Status = LeaseStatus.Renewed;
    existingLease.LeaseDuration = leaseDuration;
    // LeaseStart NICHT ändern!
    await _leaseService.UpdateLeaseAsync(existingLease);
}
else
{
    // NEUE LEASE
    var newLease = new DhcpLease { ... };
    await _leaseService.UpdateLeaseAsync(newLease);
}
```

---

### 5. **Orleans-Migration unvollständig - Grains/DhcpManager noch vorhanden**
**Schweregrad:** HOCH
**Dateien:**
- Ganzer Ordner: `/src/qt.qsp.dhcp.Server/Grains/DhcpManager/`
- Enthält: DhcpLease.cs, DhcpReservation.cs, LeaseStatus.cs, OfferGeneratorService.cs, LeaseGrainSearchService.cs

**Problem:**
- Diese Dateien sollten nach Orleans-Entfernung gelöscht sein
- Verursachen Namespace-Konflikte
- Alte Orleans-Attribute (GenerateSerializer) noch vorhanden
- Code-Duplikation mit Models-Ordner

**Auswirkung:**
- Verwir

rung welches Model zu verwenden ist
- Compiler könnte falsche Typen auswählen
- Alte Orleans-Dependencies könnten aufgerufen werden

**Lösung:**
- Kompletten Ordner Grains/DhcpManager löschen AUSSER:
  - OfferGeneratorService.cs → nach Services/ verschieben
  - LeaseGrainSearchService.cs → nach Services/ verschieben
- Alle using-Statements aktualisieren

---

## 🟡 WICHTIGE PROBLEME (Bald beheben)

### 6. **TODOs im Code nicht implementiert**
**Schweregrad:** MITTEL
**Dateien:**
- `/src/qt.qsp.dhcp.Server/Workers/NetworkListener.cs:67` - "log shutdown"
- `/src/qt.qsp.dhcp.Server/Workers/NetworkListener.cs:71` - "handle error"

**Problem:**
- Shutdown wird nicht geloggt
- Fehlerbehandlung fehlt

**Lösung:**
```csharp
// Zeile 67:
_logger.LogInformation("DHCP Network Listener shutting down");

// Zeile 71:
_logger.LogError(ex, "Error in DHCP packet processing");
```

---

### 7. **NTP Server nicht im UI editierbar**
**Schweregrad:** MITTEL
**Datei:** `/src/qt.qsp.dhcp.Server/Components/Pages/Settings.razor`

**Problem:**
- SettingsConstants.DHCP_LEASE_NTP_SERVERS existiert
- Wird in DhcpMessageHandler.cs verwendet (CreateAckMessage)
- Ist NICHT im Settings.razor UI editierbar
- Benutzer kann NTP nicht über UI konfigurieren

**Lösung:**
Settings.razor erweitern um:
```csharp
<div class="mb-3">
    <label for="ntpServers" class="form-label">NTP Servers (Optional)</label>
    <InputTextArea id="ntpServers" class="form-control" rows="2"
                   @bind-Value="editableSettings.NtpServers"
                   placeholder="132.163.96.1;132.163.97.1" />
    <div class="form-text">NTP time server IP addresses, separated by semicolons</div>
</div>
```

---

### 8. **Fehlende Datenbank-Indizes**
**Schweregrad:** MITTEL
**Datei:** `/src/qt.qsp.dhcp.Server/Data/DhcpDbContext.cs`

**Problem:**
- Keine Indizes auf häufig abgefragte Spalten
- Performance-Problem bei vielen Leases (>1000)
- Besonders kritisch: MacAddress, IpAddressString, Status

**Betroffene Queries:**
- GetLeaseByMacAsync - scannt volle Tabelle
- GetLeaseByIpAsync - scannt volle Tabelle
- GetReservationByMacAsync - scannt volle Tabelle

**Lösung:**
```csharp
modelBuilder.Entity<DhcpLease>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.IpAddressString).IsUnique();  // Häufigste Query
    entity.HasIndex(e => e.MacAddress);  // Zweit-häufigste Query
    entity.HasIndex(e => e.Status);  // Für Dashboard-Statistiken
    entity.HasIndex(e => e.LeaseStart);  // Für Expired-Queries
});

modelBuilder.Entity<DhcpReservation>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.MacAddress).IsUnique();
    entity.HasIndex(e => e.IpAddressString).IsUnique();
    entity.HasIndex(e => e.IsActive);
});
```

---

### 9. **Thread-Safety Problem in DhcpServerService**
**Schweregrad:** MITTEL
**Datei:** `/src/qt.qsp.dhcp.Server/Services/DhcpServerService.cs`

**Problem:**
- `_isRunning` und `_networkListener` werden ohne Lock modifiziert
- StartAsync/StopAsync können gleichzeitig aufgerufen werden
- Mögliche Race Condition bei Start/Stop

**Lösung:**
```csharp
private readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1);

public async Task<bool> StartAsync()
{
    await _stateLock.WaitAsync();
    try {
        if (_isRunning) return false;
        // ... Start-Logik
        _isRunning = true;
        return true;
    } finally {
        _stateLock.Release();
    }
}
```

---

### 10. **Model-Konvertierung umständlich und fehleranfällig**
**Schweregrad:** MITTEL
**Dateien:**
- `/src/qt.qsp.dhcp.Server/Services/ReservationService.cs` - ConvertToModel, ConvertToGrainModel
- `/src/qt.qsp.dhcp.Server/Services/DashboardService.cs` - ConvertToGrainModel
- `/src/qt.qsp.dhcp.Server/Components/Pages/Leases.razor` - Inline Konvertierungen

**Problem:**
- Ständiges Hin-und-Her-Konvertieren zwischen Grains.DhcpManager und Models
- Jede Konvertierung ist eine Fehlerquelle
- Overhead bei jedem API-Call

**Lösung:**
- Grains.DhcpManager Models komplett entfernen (siehe Problem #5)
- Überall nur Models verwenden
- Falls UI-spezifische ViewModels nötig: Eigene DTO-Klassen erstellen

---

## 🟢 KLEINERE PROBLEME (Nice to have)

### 11. **Fehlende Hostname/Domain-Name Implementierung**
**Schweregrad:** NIEDRIG
**Datei:** `/src/qt.qsp.dhcp.Server/Services/DhcpMessageHandler.cs`

**Problem:**
- DhcpMessage.GetHostName() und GetDomainName() werden aufgerufen
- Aber nirgendwo verarbeitet oder gespeichert
- Hostname nicht in DhcpLease oder ClientInfo gespeichert

**Lösung:**
- ClientInfo um Hostname und DomainName erweitern
- In HandleDiscoverAsync/HandleRequestAsync extrahieren und speichern
- In Dashboard/Leases-Übersicht anzeigen

---

### 12. **Import/Export JavaScript-Funktionen fehlen**
**Schweregrad:** NIEDRIG
**Dateien:**
- `/src/qt.qsp.dhcp.Server/Components/Pages/Reservations.razor` referenziert:
  - `importReservationsFile()`
  - `downloadReservations()`
- Diese JS-Funktionen existieren nicht in wwwroot

**Lösung:**
Erstelle `/wwwroot/js/reservations.js`:
```javascript
function downloadReservations(filename, content) {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
}

function importReservationsFile() {
    return new Promise((resolve) => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.json';
        input.onchange = (e) => {
            const file = e.target.files[0];
            const reader = new FileReader();
            reader.onload = (event) => resolve(event.target.result);
            reader.readAsText(file);
        };
        input.click();
    });
}
```

Dann in `_Host.cshtml` oder `App.razor` einbinden:
```html
<script src="js/reservations.js"></script>
```

---

### 13. **Keine Logging-Konfiguration für Produktion**
**Schweregrad:** NIEDRIG
**Datei:** `appsettings.json` und `appsettings.Production.json`

**Problem:**
- Vermutlich zu verbose Logging in Produktion
- Keine Log-Rotation konfiguriert
- Performance-Impact durch zu viel Logging

**Lösung:**
`appsettings.Production.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "qt.qsp.dhcp.Server": "Information"
    }
  }
}
```

---

### 14. **Fehlende Input-Validierung in Razor Components**
**Schweregrad:** NIEDRIG
**Dateien:**
- `/src/qt.qsp.dhcp.Server/Components/Pages/Leases.razor` - Keine Validierung vor RenewLease/RevokeLease
- `/src/qt.qsp.dhcp.Server/Components/Pages/Reservations.razor` - Keine IP/MAC Format-Validierung

**Lösung:**
- DataAnnotations für ViewModel-Klassen
- Validierung vor Service-Calls

---

## 📋 ZUSAMMENFASSUNG

### Nach Priorität:
| Priorität | Anzahl | Probleme |
|-----------|--------|----------|
| 🔴 KRITISCH | 5 | Doppelte Enums, Race Conditions, Lease Renewal, Orleans-Rest |
| 🟡 WICHTIG | 5 | TODOs, UI-Fehler, Performance, Thread-Safety |
| 🟢 NIEDRIG | 4 | Hostname, JS-Funktionen, Logging, Validierung |
| **GESAMT** | **14** | |

### Geschätzte Behebungszeit:
- **Kritische Probleme:** 4-6 Stunden
- **Wichtige Probleme:** 3-4 Stunden
- **Kleinere Probleme:** 2-3 Stunden
- **Testing:** 2-3 Stunden
- **TOTAL:** ~11-16 Stunden (1.5 - 2 Arbeitstage)

### Nächste Schritte (Empfohlen):
1. ✅ Problem #5: Grains/DhcpManager komplett aufräumen
2. ✅ Problem #1: LeaseStatus-Duplikation beheben
3. ✅ Problem #2: IReservationService-Duplikation beheben
4. ✅ Problem #3: Race Condition bei IP-Vergabe fixen
5. ✅ Problem #4: Lease Renewal korrekt implementieren
6. Dann die wichtigen Probleme (#6-#10)
7. Testing und Verifikation
8. Optionale Probleme (#11-#14) nach Bedarf

---

## ⚠️ RISIKO-EINSCHÄTZUNG

**Aktueller Status:** **NICHT PRODUKTIONSBEREIT**

**Hauptrisiken:**
1. IP-Konflikte durch Race Conditions (Problem #3)
2. Fehlerhaftes Lease-Management (Problem #4)
3. Namespace-Konflikte durch Duplikate (Problem #1, #2, #5)

**Nach Behebung der kritischen Probleme:** Produktionsbereit für kleinere Netzwerke (<100 Clients)

**Für große Netzwerke (>100 Clients):** Zusätzlich Probleme #8 und #9 beheben.
