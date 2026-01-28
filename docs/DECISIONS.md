# Decisions / Annahmen

Nutze dieses Dokument, um Annahmen festzuhalten, falls bei der Umsetzung Details fehlen oder unklar sind.

## Phase C Phase-Abschluss (2026-01-27 - Continuation)

### ✅ Alle 7 offenen TODOs implementiert & TEILWEISE GETESTET:

1. **Missing file playback via UI** ✅ IMPL, 🟡 TEST (Banner OK, Toast ISSUE)
   - NotificationOrchestrator smart routing: Toast wenn App versteckt, Banner wenn sichtbar
   - PlaybackMissingFile Scenario implementiert
   - VideosViewModel.PlaySelectedAsync() validiert jetzt Datei vor Playback
   - **TEST RESULT:** Banner zeigt sich, Toast NICHT (P/Invoke issue?)

2. **Missing notification for default video on autoplay** ✅ IMPL, 🟡 TEST
   - Autoplay validiert jetzt Default-Video Existenz
   - AutoplayMissingDefault Notification (Toast/Banner je nach Sichtbarkeit)
   - Implementiert in App.xaml.cs (beide Startup-Pfade)
   - **TEST RESULT:** Banner OK, Toast NICHT angezeigt

3. **Monitor selection missing (Autoplay)** ✅ IMPL, 🟡 TEST
   - Autoplay validiert jetzt Monitor-Selektion
   - AutoplayMissingMonitor Notification
   - **TEST RESULT:** Banner OK, Toast issue

4. **Remove missing-file exception** ✅ IMPL & FIXED, ✅ TEST
   - LibraryService.RemoveMediaAsync: File deletion errors caught und logged
   - DB cleanup erfolgt immer
   - VideosViewModel.RemoveSelectedAsync() cached SelectedMedia vor Reload
   - **TEST RESULT 4A:** ✅ PASS (Database bereinigt, keine Exception)
   - **TEST RESULT 4B:** ✅ PASS (Auch ohne Thumbnail kein Fehler)

5. **Thumbnails not generated on import** 🔴 ISSUE FOUND & FIXED
   - **PROBLEM:** ThumbnailServiceFFmpeg registriert (benötigt FFmpeg binary nicht vorhanden)
   - **FIX APPLIED:** ServiceCollectionExtensions.cs nun `ThumbnailService` (LibVLC)
   - ThumbnailServiceFFmpeg.cs als deprecated markiert
   - **TEST RESULT 5A-C:** ❌ FAIL (Kein Thumbnail generiert - wartend auf nächsten Test mit korrigierter Registrierung)

6. **Banner width dynamic** ✅ IMPL, ✅ TEST
   - MultiplyConverter erstellt (0.33 multiplier)
   - MainWindow.xaml: RelativeSource binding
   - Min/max constraints: 300-500px
   - **TEST RESULT 6A:** ✅ PASS (Responsive resizing funktioniert)
   - **TEST RESULT 6B:** ✅ PASS (Max 3 Banners gleichzeitig korrekt)

7. **NotificationOrchestrator smart routing** ✅ IMPL, 🟡 TEST
   - Banner (visible), Toast (hidden), Dialog (errors)
   - Neue Scenarios: PlaybackMissingFile, DefaultVideoMissing, etc.
   - **TEST RESULT 7A:** ❌ FAIL (Kein Banner angezeigt in Test A)
   - **TEST RESULT 7B:** ❌ FAIL (Kein Toast bei versteckter App)

---

## 🔴 Offene Punkte & Bekannte Issues

### Kritisch (Muss vor Release gelöst werden)

| # | Punkt | Status | Notiz |
|---|-------|--------|-------|
| **P1** | Toast-Benachrichtigungen zeigen sich nicht | 🔴 OPEN | TrayService.ShowNotification() via Shell_NotifyIcon - möglicherweise Windows 10/11 unterschiedlich |
| **P2** | Thumbnails werden nicht generiert | 🔴 OPEN | Auch mit LibVLC ThumbnailService - nächster Test wird zeigen ob Registrierungsfix ausreicht |
| **P3** | Banner in Test 7A wird nicht gezeigt | 🔴 OPEN | PlaySelectedAsync() → NotifyAsync() → ShowBannerAsync() - Debuggen notwendig |

### Mittel (Sollte gelöst werden)

| # | Punkt | Status | Notiz |
|---|-------|--------|-------|
| **P4** | PlaySelectedAsync() vs PlaybackOrchestrator dualität | 🟡 DESIGN | VideosViewModel nutzt direkt PlayerWindow statt PlaybackOrchestrator (Inkonsistenz) |
| **P5** | Single Instance + Autostart Task Scheduler | 🟡 PENDING | Gemäß SPEC 2.8, noch nicht implementiert |

### Niedrig (Nice-to-have)

| # | Punkt | Status | Notiz |
|---|-------|--------|-------|
| **P6** | H.NotifyIcon.Wpf als Toast-Alternative | 💡 SUGGESTION | Falls native P/Invoke weiterhin problematisch |
| **P7** | LibraryOrchestrator (Phase D) | 📅 FUTURE | Unified Import/Remove/SetDefault orchestration |

---

## 🧪 Zu testende Punkte (Morgen - PC2)

### Kritische Tests (MUSS HEUTE ABEND dokumentiert sein)

```
TEST-SUITE: Phase C Completion Validation

1️⃣ THUMBNAILS (mit korrigierter LibVLC Registrierung)
   [ ] Test 5A: Video importieren → Thumbnail generiert
   [ ] Check Log Output: "✓ Thumbnail generated" statt "❌ FFmpeg failed"
   [ ] Verify: Thumbnail-Datei existiert in `.thumbnails` Ordner
   [ ] Verify: UI zeigt Thumbnail in Video-Liste

2️⃣ MISSING FILE PLAYBACK (Banner-Only, bis Toast gelöst)
   [ ] Test 1A: Fenster sichtbar → Fehlendes Video → Banner "Video file not found"
   [ ] Test 1B (NICHT MÖGLICH): Toast bei versteckter App → SKIP bis Toast fixed

3️⃣ MISSING DEFAULT VIDEO (Autoplay)
   [ ] Test 2A: Autoplay aktivieren, kein Default → Banner "No default video set"
   [ ] Log Output: "❌ Autoplay: No default video set"

4️⃣ MONITOR SELECTION (Autoplay)
   [ ] Test 3A: Autoplay aktivieren, Monitor nicht gesetzt → Banner/Toast
   [ ] Log Output: "❌ Autoplay: No monitor selected"

5️⃣ REMOVE VIDEO
   [ ] Test 4A: Video mit fehlender Datei löschen → DB bereinigt, keine Exception
   [ ] Test 4B: Video mit fehlender Thumbnail löschen → OK

6️⃣ BANNER WIDTH
   [ ] Test 6A: Fenster resizen → Banner passt sich an (~1/3 Breite)
   [ ] Test 6B: 3+ Notifications → Max 3 sichtbar, sequentiell

7️⃣ SMART ROUTING (bis Toast fix)
   [ ] Test 7A: Fenster sichtbar → Banner zeigt sich
   [ ] Test 7B (SKIP): Toast - bekanntes P/Invoke Issue
```

### Debugging-Fokus für morgen

```csharp
// 1. Warum zeigt Banner sich nicht in Test 7A?
NotifyAsync() → IsMainWindowVisible() → ShowBannerAsync() → MainWindow.ShowBanner()
→ Dispatcher.Invoke() → _banners.Add(entry)

// 2. Toast - P/Invoke Shell_NotifyIcon
TrayService.ShowNotification() 
  → _nid.uFlags |= NIF_INFO
  → Shell_NotifyIcon(NIM_MODIFY, ref _nid)
  → Ballon-Notification sollte erscheinen (möglicherweise nur wenn App im Tray)

// 3. Thumbnail-Queue (mit LibVLC)
ThumbnailQueueService.ProcessQueueAsync() 
  → ThumbnailService.GenerateThumbnailAsync()
  → VLC.TakeSnapshot()
  → Datei in .thumbnails/[id].jpg
```

---

## 📝 Code Quality Check

- ✅ Build kompiliert fehlerfrei
- ✅ Exception-Handling verbessert (RemoveMediaAsync, PlaySelectedAsync)
- ✅ Logging mit Emojis für schnelleres Debugging
- ✅ CancellationToken support in Thumbnail-Queue
- ⚠️ Toast-Implementierung ist minimal (nur P/Invoke, keine Rich Features)
- ⚠️ PlaybackOrchestrator vs. VideosViewModel direkter PlayerWindow (Dualität)

---

## 🎯 Nächste Phase (Phase D - nach P1, P2, P3 behoben)

1. **LibraryOrchestrator** implementieren
   - Unified `ImportVideoAsync()`, `RemoveVideoAsync()`, `SetDefaultVideoAsync()`
   - Single point for library mutations

2. **LibraryChangeNotifier** (Event-driven)
   - Events: `OnVideoImported`, `OnVideoRemoved`, `OnDefaultVideoChanged`
   - Auto-update Tray + UI

3. **Single Instance + Autostart (Task Scheduler)**
   - Gemäß SPEC 2.8
   - Already partially implemented via `ISingleInstanceService`

---

## Build Status

✅ **CURRENT:** All changes compile successfully
🔴 **TESTING:** Banner/Toast routing needs validation + Thumbnail generation needs retry
