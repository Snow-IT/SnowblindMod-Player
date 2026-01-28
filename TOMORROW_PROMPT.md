# Prompt für PC2 - Morgen (Fortsetzung Phase C Testing & Debugging)

## Status (End of Day 2026-01-27)

**Phase:** C - TODO Completion Sprint (7/7 implemented, Partial Testing)
**Build:** ? Successfully compiling
**Last Commit:** Phase C TODO Completion: Notifications, Autoplay Validation, Thumbnails, Dynamic Banner Width

---

## ?? Aufgaben für Morgen (Priorität)

### KRITISCH (P1-P3) — Muss vor Release gelöst werden

#### **P1: Toast-Benachrichtigungen zeigen sich nicht** ??
**Problem:** 
- Tests 2B, 3B, 7B: Toast wird nicht sichtbar
- Shell_NotifyIcon P/Invoke implementiert, aber keine Balloon-Notification
- Möglicherweise nur sichtbar wenn App im Tray minimiert ist

**Debugging Path:**
```csharp
TrayService.ShowNotification() 
  ? _nid.uFlags |= NIF_INFO
  ? Shell_NotifyIcon(NIM_MODIFY, ref _nid)
  ? Windows Ballon sollte erscheinen
```

**Optionen:**
1. **Option A (Minimal):** P/Invoke fixen - correct flags/timing
   - Pros: Keine neuen Dependencies
   - Cons: Windows-Version spezifisch (10 vs 11)
   
2. **Option B (Robust):** H.NotifyIcon.Wpf library verwenden
   - Pros: Cross-platform reliable, Rich features
   - Cons: Neue dependency, höhere Komplexität
   
3. **Option C (Pragmatisch):** Toast deaktivieren für jetzt, nur Banner
   - Pros: Schnell, funktioniert
   - Cons: Weniger sichtbar wenn App im Tray

**Empfehlung:** Option A versuchen, dann B falls nötig

---

#### **P2: Thumbnails werden nicht generiert** ??
**Änderung gestern:** ServiceCollectionExtensions.cs auf `ThumbnailService` (LibVLC) umgestellt

**Zu testen:**
```
TEST 5A (Retry):
[ ] Import 2-3 Videos
[ ] Check Output:
    ?? Thumbnail enqueued: [path]
    ? Processing thumbnail: [path]
    ? Thumbnail generated: [path]
[ ] Verify: .thumbnails Ordner hat .jpg Dateien
[ ] Verify: UI zeigt Thumbnails
```

**Falls noch nicht funktioniert:**
- Check LibVLC Initialization in ThumbnailService ctor
- Verify VLC.TakeSnapshot() ist aufgerufen
- Check Media.Parse() Status

---

#### **P3: Banner wird nicht angezeigt (Test 7A)** ??
**Symptom:** PlaySelectedAsync() ? NotifyAsync() ? ShowBannerAsync() ? Aber kein Banner sichtbar

**Debug Kette:**
```csharp
// 1. PlaySelectedAsync() calls NotifyAsync
await _notifier.NotifyAsync(
    "Video file not found", 
    NotificationScenario.PlaybackMissingFile,
    NotificationType.Error);

// 2. NotificationOrchestrator.NotifyAsync() entscheidet
var isMainWindowVisible = IsMainWindowVisible();
if (isMainWindowVisible) {
    await ShowBannerAsync(...); // Should be called
}

// 3. ShowBannerAsync() calls MainWindow.ShowBanner()
public Task ShowBannerAsync(...) {
    var mainWindow = Application.Current?.MainWindow as MainWindow;
    if (mainWindow != null && mainWindow.IsVisible) {
        mainWindow.ShowBanner(...);
    }
}

// 4. MainWindow.ShowBanner() adds to _banners collection
public void ShowBanner(string message, NotificationType type, int durationMs) {
    Dispatcher.Invoke(() => {
        var entry = new BannerEntry { ... };
        _banners.Add(entry); // <- Should be visible now
    });
}
```

**Zu überprüfen:**
- [ ] IsMainWindowVisible() return true?
- [ ] MainWindow.ShowBanner() ist aufgerufen?
- [ ] _banners ItemsControl ist richtig gebunden?
- [ ] Banner-Template in MainWindow.xaml ist sichtbar?

---

### MITTEL (Design-Verbesserungen) — Phase D Kandidaten

#### **P4: Dualität PlaySelectedAsync vs PlaybackOrchestrator**
```
Aktuell:
- VideosViewModel.PlaySelectedAsync() ? öffnet PlayerWindow direkt
- PlaybackOrchestrator.PlayVideoAsync() ? offizielle Playback-Entry point

Sollte sein:
- VideosViewModel.PlaySelectedAsync() ? ruft PlaybackOrchestrator.PlayVideoAsync() auf
```

**Action:** Notieren für Phase D (LibraryOrchestrator sprint)

---

## ?? Volle Test-Suite (Checkliste)

```
THRESHOLD: Mindestens P1, P2 MÜSSEN PASS sein vor Commit

1?? THUMBNAILS (mit LibVLC)
   [ ] Test 5A: Import Video ? Thumbnail generiert
   [ ] Check .thumbnails Ordner
   [ ] UI zeigt Thumbnail-Bild

2?? MISSING FILE UI (KNOWN: Toast issue)
   [ ] Test 1A: Fenster sichtbar ? Banner "Video file not found"
   [ ] Test 1B: SKIP (Toast bekanntes Issue)

3?? MISSING DEFAULT (KNOWN: Toast issue)
   [ ] Test 2A: Autoplay ? Banner "No default video set"

4?? MISSING MONITOR (KNOWN: Toast issue)
   [ ] Test 3A: Autoplay ? Banner "No monitor selected"

5?? REMOVE VIDEO
   ? Test 4A: PASS
   ? Test 4B: PASS

6?? BANNER WIDTH
   ? Test 6A: PASS
   ? Test 6B: PASS

7?? SMART ROUTING
   [ ] Test 7A: Fenster sichtbar ? Banner OK
   [ ] Test 7B: SKIP (Toast issue)
```

---

## ?? Debugging Tools & Commands

### Output Window Logs
```
Visual Studio ? Debug ? Windows ? Output (Strg+Alt+O)
Filter: "Thumbnail", "Notification", "Autoplay", "Toast"
```

### Key Log Messages zu suchen
```
? = Success (Green checkmark)
? = Fehler (Red X)
? = Timeout
?? = Retry/Fallback
? = Warning

Thumbnails:
  "?? Thumbnail enqueued" ? In queue
  "? Processing thumbnail" ? Wird verarbeitet
  "? Thumbnail generated" ? Erfolgreich
  "? All FFmpeg fallbacks failed" ? Komplett fehlgeschlagen

Notifications:
  "ShowBannerAsync called" ? Banner sollte sichtbar sein
  "IsMainWindowVisible: true/false" ? Fenster-Status
  "ShowTrayToastAsync called" ? Toast sollte sichtbar sein
```

### Settings für Testing
```json
{
  "AutoplayEnabled": true,
  "AutoplayDelaySeconds": 1,
  "MinimizeToTrayOnStartup": false,
  "DefaultMonitorId": "YOUR_MONITOR_ID",
  "DefaultVideoId": ""
}
```

---

## ?? Wichtige Dateien (heute geändert)

**Core Changes:**
- `src/SnowblindModPlayer.Infrastructure/ServiceCollectionExtensions.cs` — ThumbnailService registrierung
- `src/SnowblindModPlayer.Core/Services/INotificationOrchestrator.cs` — Neue Scenarios
- `src/SnowblindModPlayer.App/Services/NotificationOrchestrator.cs` — Smart routing

**UI Changes:**
- `src/SnowblindModPlayer.App/ViewModels/VideosViewModel.cs` — PlaySelectedAsync, RemoveSelectedAsync fixes
- `src/SnowblindModPlayer.App/MainWindow.xaml` — Dynamic banner width
- `src/SnowblindModPlayer.App/Converters/MultiplyConverter.cs` — New converter

**Documentation:**
- `docs/DECISIONS.md` — Test results, open issues, test suite
- `.github/copilot-instructions.md` — Implementation state, known issues
- `COMMIT_MESSAGE.md` — Vorbereitete Commit-Nachricht

---

## ?? Nächste Schritte (falls P1-P3 gelöst)

1. **Tests bestätigen + Commit**
2. **Phase D vorbereiten:**
   - LibraryOrchestrator design
   - Event-driven UI updates
   - Single Instance + Autostart

---

## ?? Kontext-Info für schnellen Einstieg

**Was ist Phase C?**
- Tray Integration + Autoplay + Fehlerbehandlung
- 7 TODO-Items, 6 abgeschlossen, 1 Registrierungsfix nötig

**Was sind die 3 kritischen Issues?**
1. **Toast:** P/Invoke Shell_NotifyIcon zeigt Balloons nicht
2. **Thumbnails:** Warten auf Test-Retry mit LibVLC
3. **Banner:** Möglicherweise MainWindow Context-Issue

**Wichtigste Dateien zum Verstehen:**
- `NotificationOrchestrator.cs` — Brain der Benachrichtigungen
- `PlaybackOrchestrator.cs` — Brain der Wiedergabe
- `VideosViewModel.cs` — Brain der UI-Interaktionen

---

## ?? Falls du steckenbleibst

**Toast nicht sichtbar?**
? Check `TrayService.ShowNotification()` flags + timing
? Überprüfe ob TrayIcon überhaupt initialisiert ist
? Versuche Option B (H.NotifyIcon.Wpf)

**Thumbnails nicht generiert?**
? Check LibVLC Init in ThumbnailService ctor
? Verify VLC.TakeSnapshot() return true
? Check .thumbnails Folder existiert

**Banner nicht sichtbar?**
? Debug Kette: PlaySelectedAsync ? NotifyAsync ? IsMainWindowVisible ? ShowBannerAsync
? Add debug.writeline() in jedem Schritt
? Check MainWindow als MainWindow castbar?

---

## Git Commit (Ready-to-go)

Siehe `COMMIT_MESSAGE.md` im Repo-Root

**Kurz-Version:**
```
git add -A
git commit -m "Phase C TODO Completion: Notifications, Autoplay Validation, Thumbnails, Dynamic Banner Width

- Fixed: ThumbnailService registration (LibVLC instead of FFmpeg)
- Enhanced: NotificationOrchestrator smart routing (Banner/Toast/Dialog)
- Improved: VideosViewModel file validation + exception handling
- Added: Autoplay validation (Default video + Monitor selection)
- Added: Dynamic banner width (~1/3 app width via MultiplyConverter)

Known Issues (P1-P3): Toast visibility, Thumbnail generation verification, Banner display in edge cases
See docs/DECISIONS.md for full testing status."
```

---

**Viel Erfolg morgen! ??**
