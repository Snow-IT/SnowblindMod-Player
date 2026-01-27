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

## Current Implementation State (Phase C - Tray Integration)

### Completed
- ✓ Tray icon (transparent background, native P/Invoke)
- ✓ Tray context menu: Show → Play Default → Play Video [Dynamic] → Stop → Exit
- ✓ PlaybackOrchestrator: unified async entry point for all playback scenarios
- ✓ Monitor selection respected (PlayerWindow.PositionOnSelectedMonitor)
- ✓ Tray menu video list sorted (default first, then alphabetical per SPEC 5.2)

### Next (Immediate)
- Autoplay on startup (uses PlaybackOrchestrator.PlayDefaultVideoAsync)
- VideosView UI integration (doppelklick via PlaybackOrchestrator.PlayVideoAsync)

## Agent behavior

- When you identify potential concerns, tradeoffs, or improvement opportunities (including during planning/spec interpretation), always present 2-3 concrete options with pros/cons and a recommendation. Do not mention 'alternative strategies' without listing them.
- Implement directly (agent style). Make changes in small batches.
- After edits: ensure solution builds; add minimal tests when cheap.
- If ambiguous: document assumptions in docs/DECISIONS.md (do not change requirements).
- **ALWAYS update docs/DECISIONS.md with new architectural decisions, design patterns, or known limitations.**
- **ALWAYS update this file (copilot-instructions.md) when tech decisions or implementation state changes.**
- Proactively present explicit alternative strategies/options (2-3) with pros/cons and a recommendation whenever potential concerns or improvements arise, including during planning and spec interpretation.

## Implementation order (Risk-first)
1) ✓ Playback spike + hotkeys (COMPLETE)
2) ✓ Multi-monitor selection + fullscreen (COMPLETE)
3) ✓ Storage hybrid + cleanup (COMPLETE)
4) ✓ Import/remove + thumbnail queue (IN PROGRESS)
5) ✓ Videos UI list/tile + toolbar (IN PROGRESS)
6) ✓ Tray + autoplay + single instance + autostart (Tray COMPLETE, Autoplay/SingleInstance/Autostart PENDING)
7) Logs UI + file viewer (PENDING)
8) GitHub Actions portable ZIP (PENDING)

## Architecture Reference

### PlaybackOrchestrator (NEW - Phase C)
- **File:** `src/SnowblindModPlayer.App/Services/PlaybackOrchestrator.cs`
- **Purpose:** Unified orchestration of video playback across all scenarios
- **Methods:**
  - `PlayVideoAsync(videoId)`: Play specific video (opens PlayerWindow, applies settings, starts playback)
  - `PlayDefaultVideoAsync()`: Play default/favorite video
  - `ApplyPlaybackSettingsAsync()`: Sync volume, mute, loop from settings service
- **Usage:** Tray menu, future UI clicks, future Autoplay
- **Design Pattern:** Service Orchestrator / Facade (combines multiple services into single unified interface)

### PlaybackService (Existing - Phase A/B)
- **File:** `src/SnowblindModPlayer.Infrastructure/Services/PlaybackService.cs`
- **Design:** Singleton, no DI (intentional: only ONE MediaPlayer instance per app)
- **Responsibility:** Low-level LibVLC playback (Play/Pause/Stop/Seek/Volume/Mute)
- **Do NOT add DI:** Would violate single-instance constraint

### TrayService (Phase C)
- **File:** `src/SnowblindModPlayer.App/Services/TrayService.cs`
- **Implementation:** Native Windows P/Invoke Shell_NotifyIcon (no external UI framework)
- **Callbacks:** Routes to PlaybackOrchestrator for Play/Stop operations
- **Menu Structure:** Populated dynamically from ILibraryService
