# Phase C + D Milestone: Notifications Complete + LibraryOrchestrator Foundation

## Phase C: COMPLETE ?
- Tray icon (native P/Invoke Shell_NotifyIcon, transparent)
- Tray context menu (Show, Play Default, Play Video [dynamic], Stop, Exit)
- Playback orchestration (unified PlaybackOrchestrator for all scenarios)
- Autoplay on startup (configurable delay, monitor/default validation)
- Notifications system (Banner + Toast + Dialog, smart routing)
- All notification scenarios wired (Import/Remove/Default/Playback/Autoplay/Settings/Monitor/MinimizeToTray)
- Thumbnails (LibVLC-based, sequential queue, fallback placeholder)
- Theme-aware notification colors (Light/Dark)
- Banner animations (fade-out + slide-up)
- Toast window (custom popup, unten rechts, 6s auto-dismiss)

## Phase D: Foundation Laid ?
- ILibraryOrchestrator interface created (unified Import/Remove/SetDefault)
- LibraryOrchestrator implementation (event-driven, auto-notify)
- Event system (VideoImported, VideoRemoved, DefaultVideoChanged)
- DI registration ready

## What's Next (Separate PRs)
- [ ] Wire VideosViewModel to LibraryOrchestrator
- [ ] Wire TrayService to library events (auto-update)
- [ ] Logs UI (LogsViewModel + viewer)
- [ ] Single instance + autostart (optional)

## Build
? Compiles without errors
? All notifications functional
? Autoplay validated
? Import + Remove + Settings all trigger toasts

## Test Coverage
See TESTLIST_PHASE_CD.md for detailed test scenarios
