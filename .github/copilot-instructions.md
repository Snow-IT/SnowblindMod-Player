# Snowblind-Mod Player — Repo Instructions (MUST FOLLOW)

You are implementing the Windows desktop app "Snowblind-Mod Player".  
Follow docs/SPEC_FINAL.md strictly.

## Fixed Tech Decisions (do not change)
- UI: WPF (.NET 8+), MVVM + Services; implement an app-wide design system with global WPF styles/templates consistent across Light/Dark themes, where only colors change, not layout/controls.
- Playback: LibVLC via VLCSharp, separate PlayerWindow, single MediaPlayer instance (Singleton PlaybackService, no DI)
- Playback Orchestration: **PlaybackOrchestrator** service for unified video playback entry point (all scenarios: Tray, UI, Autoplay)
- Thumbnails: VLC snapshot at 5% duration; fallback 1s/0s; async queue max 1 parallel; timeout+retry; on failure placeholder+warn; import still succeeds
- Storage: Hybrid (Settings JSON, Library SQLite via Microsoft.Data.Sqlite); originalSourcePath UNIQUE
- Tray: Native Windows Shell_NotifyIcon P/Invoke (no H.NotifyIcon.Wpf); Close-to-tray; exit only via tray; context menu via TrackPopupMenuEx
- Autostart: Windows Task Scheduler LogonTrigger; app starts with --tray
- Single instance: Mutex + IPC (NamedPipe); second start focuses existing, no autoplay
- Logging: Serilog daily rolling file (YYYY-MM-DD.log), level live changeable
- Packaging: Portable ZIP via GitHub Actions (tag v*)

## Current Implementation State (Phase C-D Continuation)

### ✅ Completed (Phase C - TODO Completion Sprint)
- ✓ Tray icon (transparent background, native P/Invoke)
- ✓ Tray context menu: Show → Play Default → Play Video [Dynamic Submenu] → Stop → Exit
- ✓ PlaybackOrchestrator: unified async entry point for all playback scenarios
- ✓ Monitor selection respected (PlayerWindow.PositionOnSelectedMonitor)
- ✓ Tray menu video list sorted (default first, then alphabetical per SPEC 5.2)
- ✓ Autoplay on startup (configurable delay via AutoplayDelayMs setting)
- ✓ **Phase C COMPLETION (2026-01-27):**
  - ✓ NotificationOrchestrator smart routing (Banner/Toast/Dialog based on window visibility)
  - ✓ Missing file playback notification (UI) with validation + proper scenarios
  - ✓ Missing default video notification (Autoplay) with validation + skip
  - ✓ Monitor selection validation (Autoplay) with skip + notification
  - ✓ Remove missing-file exception handling (graceful DB cleanup)
  - ✓ Dynamic banner width (~1/3 app width via MultiplyConverter)
  - ✓ VideosViewModel fixes (PlaySelectedAsync file validation, RemoveSelectedAsync null-safety)
  - ✓ ThumbnailService registration fixed (LibVLC instead of FFmpeg)

### 🟡 In Progress / Testing Required
- **Notifications (P1):** Toast via Shell_NotifyIcon may not be visible (P/Invoke, Windows 10/11 differences)
- **Thumbnails (P2):** LibVLC registration now correct; test import again to verify generation
- **Banner Display (P3):** Test case 7A shows banner not displayed; debug NotifyAsync() routing

### ⏳ Pending (Phase D / Future)
- LibraryOrchestrator (unified Import/Remove/SetDefault)
- LibraryChangeNotifier (event-driven UI sync)
- Single Instance + Autostart (Task Scheduler - PENDING)
- Logs UI + file viewer (PENDING)

---

## Known Issues & Debugging Focus

### Critical (Phase C Completion Blockers)

1. **Toast Notifications (P1)**
   - Implementiert: `TrayService.ShowNotification()` via `Shell_NotifyIcon` P/Invoke
   - **Problem:** Nicht sichtbar in Tests 2B, 3B, 7B
   - **Ursache:** P/Invoke Notification nur wenn App im Tray? Windows 10 vs 11 issue?
   - **Workaround:** Banner-only für jetzt, bis Toast gelöst
   - **Alternative:** H.NotifyIcon.Wpf library (höhere Komplexität)

2. **Thumbnails Generation (P2)**
   - **FIXED:** ServiceCollectionExtensions.cs jetzt `ThumbnailService` (LibVLC) statt FFmpeg
   - **Next:** Test 5 wieder ausführen um zu verifizieren dass Thumbnails generiert werden
   - Queue arbeitet sequentiell (max 1 parallel), Timeout 10s, Retry 2x
   - Fallback: VLC → Placeholder (wenn alles fehlschlägt)

3. **Banner Display in UI (P3)**
   - **Test 7A Result:** Kein Banner angezeigt (sollte aber!)
   - **Debug Path:** PlaySelectedAsync() → NotifyAsync(PlaybackError) → IsMainWindowVisible() → ShowBannerAsync()
   - Möglich: `MainWindow` nicht korrekt konfiguriert oder `ShowBanner()` Call fehlschlagen

### Design Issues (Mittel-Priorität)

- **VideosViewModel vs PlaybackOrchestrator:** Zwei Playback-Eintrittspunkte (Dualität)
  - VideosViewModel.PlaySelectedAsync() öffnet PlayerWindow direkt
  - PlaybackOrchestrator.PlayVideoAsync() ist offizielle Eintrittspunkt
  - → Sollten vereinheitlicht werden (Phase D)

---

## Test Suite für Morgen

Siehe `docs/DECISIONS.md` → Abschnitt "🧪 Zu testende Punkte (Morgen - PC2)"

**Kritische Tests:**
1. Thumbnails generieren (mit neuer Registrierung)
2. Missing file Playback → Banner (nur, bis Toast gelöst)
3. Missing default Autoplay → Notification
4. Monitor missing Autoplay → Notification
5. Remove missing files → Graceful
6. Banner width responsive
7. Smart routing Banner (7A)

---

## Implementation Notes

### Architecture Patterns Used
- **Service Orchestrator:** PlaybackOrchestrator für unified playback
- **Smart Notification Routing:** Context-aware Banner/Toast/Dialog
- **Exception-Safe Operations:** RemoveMediaAsync, PlaySelectedAsync mit Validierung
- **Sequential Queue:** ThumbnailQueueService (max 1 parallel, timeout, retry)
- **Value Converter:** MultiplyConverter für responsive UI binding

### Files Modified (Phase C Completion)
- `NotificationOrchestrator.cs` → Smart routing implementation
- `PlaybackOrchestrator.cs` → New scenarios, better error messages
- `LibraryService.cs` → Exception-safe file deletion
- `ThumbnailQueueService.cs` → CancellationToken, enhanced logging
- `ThumbnailService.cs` → CancellationToken support
- `VideosViewModel.cs` → File validation, null-safety fixes
- `App.xaml.cs` → Autoplay validation (Default + Monitor)
- `MainWindow.xaml` → Dynamic banner width binding
- `ServiceCollectionExtensions.cs` → **CRITICAL FIX:** LibVLC ThumbnailService registration
- Created: `MultiplyConverter.cs`

### Git Commits This Session
1. Main: "Phase C TODO Completion Sprint (7 items): Notifications, Thumbnails, Autoplay, Exception-safe Remove"
2. Consider: Separate commit for "Fix: ThumbnailService registration (LibVLC instead of FFmpeg)" if needed
