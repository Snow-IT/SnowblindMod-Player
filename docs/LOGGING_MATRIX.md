# Logging Matrix

## Logging Policy
- **Debug**: Bootstrap/background activity (developer diagnostics)
- **Info**: User-triggered actions and expected outcomes
- **Warn**: Missing prerequisites or recoverable constraints
- **Error**: Unexpected failures that should not happen in normal flow
- **Critical**: Unhandled exceptions or fatal crashes

## Startup / Background (Debug)
- `=== APPLICATION STARTUP ===`
- `Log path: ...`
- `DI container built`
- `Primary instance acquired`
- `AppData paths initialized`
- `Database initialized`
- `Tray: Tray icon added`
- `Notify: scenario=... type=... visible=...`
- `Notify: Banner: ...`
- `Notify: Toast: ...`
- `Playback: Settings applied: Volume=... Muted=... Loop=...`
- `Tray: Toast: <title> - <message>`

## User Actions (Info)
- `Library: Import X file(s)`
- `Library: Imported X video(s)`
- `Library: Remove video <id>`
- `Library: Removed: <name>`
- `Library: Set default video <id>`
- `Library: Default set: <name>`
- `Playback: Playback started: <name>`
- `Settings: Settings saved`
- `Settings: Theme changed: <pref>`
- `Settings: Monitor set: <display>`
- `Tray: Show requested`
- `Tray: Play default requested`
- `Tray: Play video requested: <name>`
- `Tray: Stop requested`
- `Tray: Exit requested`

## Preconditions Missing (Warn)
- `Library: No videos imported (invalid or duplicate)`
- `Playback: Video not found: <id>`
- `Playback: Missing file: <name>`
- `Playback: No monitor selected - playback skipped`
- `Playback: Default file missing: <name>`
- `Playback: No default video set`

## Unexpected Failures (Error)
- `Library: Import failed: <message>`
- `Library: Remove failed: <message>`
- `Library: Set default failed: <message>`
- `Playback: Player window not available`
- `Playback: Playback failed: <message>`
- `Playback: PlayDefault failed: <message>`
- `Playback: Apply settings failed: <message>`
- `Notify: Banner failed: <message>`
- `Notify: Toast failed: <message>`
- `Tray: Failed to create tray window`
- `Tray: Failed to add tray icon`
- `Tray: Menu command error: <message>`
- `Tray: ShowNotification failed: <message>`

## Critical (Fatal)
- `UNHANDLED EXCEPTION`
- `UNHANDLED APPDOMAIN EXCEPTION`

## Default Error Routing
- `INotificationOrchestrator.NotifyErrorAsync(...)`
  - Uses `NotificationScenario.Generic` by default.
  - Logs the error and emits a user-facing fallback code if no specific scenario exists.
