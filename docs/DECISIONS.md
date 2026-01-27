# Decisions / Annahmen

Nutze dieses Dokument, um Annahmen festzuhalten, falls bei der Umsetzung Details fehlen oder unklar sind.

- 2026-01-26: Backlog in `docs/ISSUES_BACKLOG.md` basiert auf `docs/SPEC_FINAL.md`, `docs/ARCHITECTURE.md`, `docs/IMPLEMENTATION_PLAN.md`, `docs/TEST_CHECKLIST.md`, `docs/DATA_MODEL.md`.
- 2026-01-26: Schema-Entscheidung: Beibehaltung der bestehenden SQLite-Tabelle `Media`.
- 2026-01-26: M3-1 Settings Coverage abgeschlossen (8 Keys mit Defaults).
- 2026-01-26: **Option B - VLC Implementation (CURRENT STATE):** Clean, minimal XAML; --repeat + --loop flags; Position=0 fallback for loop. Build successful.
- **2026-01-27 (Phase C - Tray Integration):**
  - **PlaybackOrchestrator** implemented as unified entry point for all video playback scenarios (Tray, UI, future Autoplay).
  - **PlaybackService** remains Singleton (no DI) by design: only ONE player window + MediaPlayer instance allowed globally.
  - Tray menu (Play Default, Play Video, Stop) now routes through PlaybackOrchestrator for consistency.
  - Monitor selection (SPEC 3.1) properly respected: PlayerWindow.PositionOnSelectedMonitor() called on Show().
  - Tray icon transparent (native Windows P/Invoke Shell_NotifyIcon).

## Known Limitations (LibVLCSharp.WPF)

**Current Status:** Playback works, hotkeys work, but two quirks remain:

1. **Loop:** Video stops at end instead of auto-restarting
   - Cause: LibVLCSharp.WPF doesn't reliably handle --loop/--repeat in embedded mode
   - Flags used: --repeat + --loop + Position=0 fallback (none fully reliable)
   - Workaround: Implement UI button or accept current behavior
   - Solution (Option 2): Use --no-embedded-video (separate VLC process) - proven in PowerShell script

2. **Stretch on 4:3 Monitor:** Black bars appear on 1600x1200 monitor
   - Cause: MediaPlayer.AspectRatio=null + Scale=0 ignored on non-16:9 monitors
   - Flags tried: --autoscale, --aspect-ratio=, --crop=, --monitor-par=1:1
   - Solution (Option 2): Use --no-embedded-video + --video-x/y positioning

**Decision:** Accept current state for Phase C (Tray). Implement Option 2 (separate VLC process) as future task if needed.

## Implementation Summary

### Architecture: Playback Orchestration

**PlaybackOrchestrator.cs** (NEW)
- Unified entry point: `PlayVideoAsync(videoId)` and `PlayDefaultVideoAsync()`
- Handles: Opens PlayerWindow, applies settings (volume, mute, loop, monitor selection), starts playback
- All playback scenarios (Tray, UI, Autoplay) use this single service
- **Benefit:** DRY principle - playback logic changes affect only one place

**PlaybackService.cs** (Singleton, No DI)
- Wraps LibVLC MediaPlayer
- Single instance per app (no multiple players allowed)
- VLC flags: --repeat, --loop, --autoscale, --no-osd, --quiet, --disable-screensaver
- EndReached handler with Position=0 fallback
- Loop controlled via _loopEnabled flag

### PlayerWindow.xaml
- Only `<vlc:VideoView>` + black background (minimal XAML)
- No overlays, no error message controls
- Direct hotkey handling: Space, Arrows, M, L, F11, ESC
- PositionOnSelectedMonitor() called in Loaded event (respects SPEC 3.1)

### Tray Service Integration
- ITrayService now accepts async Task delegates for Play/Stop callbacks
- Tray context menu: Show ? Play Default ? Play Video [Submenu] ? Stop ? Exit
- Dynamic video list sorted: Default video first (?), then alphabetical (SPEC 5.2)
- Icon: Native Windows Shell_NotifyIcon with P/Invoke, transparent background

### Features Verified
- ? Playback starts correctly
- ? Hotkeys work (pause, seek, volume, mute, loop toggle, fullscreen, escape)
- ? Multi-monitor fullscreen (DPI-aware)
- ? Fullscreen-on-start from settings
- ? Tray icon visible with transparency
- ? Tray menu functional (Show, Play Default, Play Video, Stop, Exit)
- ? Monitor selection respected (tray playback opens on selected monitor)
- ? Loop (quirky - doesn't auto-restart reliably)
- ? Scaling on 4:3 monitor (black bars appear)

## Next Phase: Autoplay & Future Enhancements

### Autoplay Implementation (SPEC 2.6)
- Will use: `await playbackOrchestrator.PlayDefaultVideoAsync()`
- Startup delay: await Task.Delay(delayMs) before orchestrator call
- Monitor validation: Check selected monitor exists; skip autoplay if not

### Future: VideosView UI Integration
- Doppelklick on video: `await playbackOrchestrator.PlayVideoAsync(videoId)`
- Uses same path as Tray Play Video (unified playback flow)

## Potential Centralizations & Improvements (NOT YET IMPLEMENTED)

**Recommendation:** Review these before committing to future phases. Consider implementing as part of Phase D/E:

### 1. **LibraryOrchestrator** (Candidate for Phase D: Import/Remove)
**Current State:** Import/Remove logic scattered across ImportService + LibraryService + VideosViewModel
**Proposed:** Create `LibraryOrchestrator` for unified library mutations
- `ImportVideoAsync(sourcePath)`: Copy file, create entry, generate thumbnail, update UI
- `RemoveVideoAsync(videoId)`: Delete file, delete entry, cleanup thumbnail, update UI/Tray
- `SetDefaultVideoAsync(videoId)`: Update settings, notify UI/Tray
**Benefit:** Single point of control for all library changes; UI/Tray updates always in sync
**Files affected:** ImportService, VideosViewModel, VideoLibraryService, TrayService

### 2. **NotificationOrchestrator** (Candidate for Phase E/F: UI Integration)
**Current State:** Error handling scattered - MessageBox in App.xaml.cs, Debug.WriteLine everywhere
**Proposed:** Unified notification service
- `NotifyAsync(title, message, type)`: Route to Tray OR UI (depending on window visibility)
- Types: Info, Warning, Error, Success
- Centralized exception handling + logging
**Benefit:** Consistent user feedback across all operations
**Files affected:** ImportService, PlaybackOrchestrator, VideosViewModel, App.xaml.cs

### 3. **WindowOrchestrator** (Candidate for Phase E: UI Windows)
**Current State:** Window opening scattered - PlayerWindow in PlaybackOrchestrator, MonitorSelection in Settings
**Proposed:** Unified window lifecycle management
- `OpenPlayerWindowAsync(videoId)`: Encapsulates monitor selection, settings application
- `ClosePlayerWindowAsync()`: Cleanup + event routing
- `OpenDialogAsync()`: Settings, import, etc.
**Benefit:** Single point for all window state management
**Files affected:** PlaybackOrchestrator, SettingsView, VideosViewModel

### 4. **SettingsChangeListener** (Candidate for any phase)
**Current State:** Settings changes don't immediately affect playback (volume, loop, etc)
**Proposed:** Auto-apply settings to live playback when changed
- `SettingsService.RegisterLiveUpdate()` for Volume, Mute, Loop ? immediately call `PlaybackService.SetVolumeAsync()` etc.
- Tray updates on settings changes
**Benefit:** Real-time responsiveness; no need to restart playback
**Files affected:** SettingsService, PlaybackService, PlaybackOrchestrator

### 5. **LibraryChangeNotifier** (Candidate for Phase D)
**Current State:** When import/remove happens, Tray menu doesn't auto-update
**Proposed:** Event-driven library change system
- `ILibraryService` publishes events: `OnVideoImported`, `OnVideoRemoved`, `OnDefaultVideoChanged`
- Subscribers (TrayService, VideosViewModel, etc.) auto-update their state
**Benefit:** Automatic consistency; no manual UI refresh calls needed

## Decision: Documentation & Continuous Improvement

**Rule going forward:**
- After EVERY implementation: Immediately document in DECISIONS.md
- Before EVERY new phase: Review potential centralizations above
- When creating Orchestrators: Consider if similar pattern applies elsewhere
- Don't wait for user reminder to document or improve

**This phase (Phase C) taught us:** Playback needed centralization ? PlaybackOrchestrator. Apply same thinking to Import, Notifications, Windows, Settings, and Library changes.

### NotificationOrchestrator - Specification & Design Decisions

**2026-01-27 - User Input on Notification Strategy:**

#### Design Decisions
1. **Toast Notifications (Windows Native)**: Only for "App minimized to tray" scenario - no progress toasts
2. **Autoplay Toast**: New scenario - "SnowblindMod-Player hat Default-Video: [name] gestartet!"
3. **Banner Stack**: Max 3 simultaneous, rest queued sequentially
   - Animation: Smooth fade-out (top), slide-up remaining, new appends bottom
   - Duration: 5-7 seconds (customizable per notification type)
4. **No Sound/Vibration**: App not critical enough; visual feedback sufficient
5. **Notification Routing:**
   - MainWindow **visible** ? Banner (UI-integrated)
   - MainWindow **hidden** ? Tray Toast (Windows native)
   - **Errors** ? Dialog (blocking) + Log + Banner (if window visible)
   - **Confirmations** ? Dialog (blocking) with Yes/No

#### Notification Types Matrix

Below is the **complete notification inventory**. User to mark preferred display method:

| # | Scenario | Text (German) | Banner | Dialog | Toast | Log | Notes |
|---|----------|---------------|--------|--------|-------|-----|-------|
| **IMPORT** |
| 1 | Video imported successfully | "Video erfolgreich importiert: [name]" | X | - | - | X | |
| 2 | Import failed - file not readable | "Fehler beim Import: Datei nicht lesbar" | X | - | - | X | |
| 3 | Import - duplicate detected | "Video existiert bereits: [name]" | X | X | - | X | Choose if should be imported anyway |
| 4 | Import - thumbnail generation failed | "Thumbnail konnte nicht generiert werden" | X | - | - | X |  |
| **REMOVE** |
| 5 | Delete confirmation | "Video wirklich l�schen? Datei wird permanent gel�scht." | - | X | - | - |  |
| 6 | Delete successful | "Video gel�scht: [name]" | X | - | - | X | |
| 7 | Delete failed | "Fehler beim L�schen: [reason]" | X | - | - | X |  |
| **PLAYBACK** |
| 8 | No default video set | "Kein Standard-Video definiert" | X | - | X | X | When user clicks "Play Default" or Autoplay is active and the app starts |
| 9 | Selected monitor not available | "Monitor nicht verf�gbar - Autoplay �bersprungen" | X | - | X | X | skip playback |
| 10 | Autoplay started (NEW) | "SnowblindMod-Player hat Standard-Video gestartet: [name]" | X | - | X | X | |
| **DEFAULT VIDEO** |
| 11 | Set as default | "Als Standard-Video gespeichert: [name]" | X | - | - | X | |
| 12 | Default cleared | "Standard-Video zur�ckgesetzt" | X | - | - | X | we dont have this option, but may be useful if video was deleted or not found |
| **TRAY** |
| 13 | Minimize to tray | "SnowblindMod-Player l�uft im Tray weiter" | - | - | X | X | Windows native toast (5-7s) |
| **SETTINGS** |
| 14 | Monitor selection invalid | "Ausgew�hlter Monitor existiert nicht" | X | - | - | X | |
| 15 | Settings saved | "Einstellungen gespeichert" | X | - | - | X | subtle. maybe with a much shorter display time then other banners |
| **LIBRARY/AUTOSTART** |
| 16 | Library loaded at startup | "Bibliothek geladen: X Videos" | - | - | - | X |  |

#### Design Details for NotificationOrchestrator

**Implementation Path:**
```csharp
public interface INotificationOrchestrator
{
    // Banner (max 3, stacked + sequential queue)
    Task ShowBannerAsync(string message, NotificationType type = Info, int durationMs = 5000);
    
    // Dialog (blocking, for critical decisions)
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    
    // Tray Toast (Windows native, auto-dismiss)
    Task ShowTrayToastAsync(string title, string message, int durationMs = 6000);
    
    // Automatic routing based on context
    Task NotifyAsync(string message, NotificationScenario scenario);
}

public enum NotificationScenario
{
    ImportSuccess,
    ImportError,
    RemoveSuccess,
    RemoveError,
    PlaybackError,
    DefaultVideoSet,
    AutoplayStarted,
    MinimizeToTray,
    SettingsSaved,
    // ... etc
}
```

**Routing Logic (Smart Context Awareness):**
```
IF scenario == MinimizeToTray
  ? Show as Tray Toast (Windows native)
  
ELSE IF scenario is Error AND (no MainWindow OR MainWindow hidden)
  ? Show as Tray Toast + Log
  
ELSE IF scenario is Error AND MainWindow visible
  ? Show as Dialog + Banner + Log
  
ELSE IF MainWindow visible
  ? Show as Banner
  
ELSE (MainWindow hidden)
  ? Log only (silent)
```
#### Next Steps
1. **User confirms/modifies table above** (mark ? for desired display methods)
2. **Define German text for all rows** (confirm copy is good)
3. **Implement NotificationOrchestrator** with Banner + Dialog + Toast services
4. **Add to Phase D** (LibraryOrchestrator) or separate quick win
5. **Refactor existing error handling** to use NotificationOrchestrator

## Open TODOs (next session)
- [ ] Missing file playback via UI: currently opens black window; show notification (Toast/Banner) even when main window hidden/tray. Also surface same toast path as Tray Play Video.
- [ ] Missing notification for manual delete: play from UI when file missing should notify.
- [ ] Missing notification for default video deleted on autoplay start; expect banner/toast "No default video set".
- [ ] Remove missing-file exception: deleting video in app still throws when file already gone.
- [ ] Thumbnails not generated on import (investigate queue/FFmpeg paths, snapshot timing per spec: 5% with fallbacks 1s/0s).
- [ ] Banner width: make dynamic (~1/3 app width) instead of fixed 420px; avoid covering buttons.
- [ ] Monitor selection missing (settings.json cleared): autoplay should skip playback and notify "No monitor selected" (currently may still start on primary).
