# Implementierungsplan (Milestones)

Reihenfolge: Risk-first.

## Milestone 0 — Repo & Skeleton
- Solution/Folder Layout, WPF Shell, DI/Service Registrierung, AppData Pfade

## Milestone 1 — Playback Spike (LibVLC)
- `PlaybackService` + `PlayerWindow` + Hotkeys/Maus gemäß Spec

## Milestone 2 — Multi‑Monitor + Fullscreen
- `MonitorService`, Monitorlayout UI, Fullscreen auf Zielmonitor

## Milestone 3 — Storage Hybrid
- Settings JSON (Defaults, live), Library SQLite (Schema, CRUD, E1 Cleanup)

## Milestone 4 — Import/Remove + Thumbnails
- Import (Whitelist, Duplikat B1, Copy, C1, Persist)
- ThumbnailQueue (5% + fallback, timeout/retry, placeholder)

## Milestone 5 — Videos UI
- List/Tile + persistierter ViewMode, Toolbar fixiert, Default Badge

## Milestone 6 — Tray/Autoplay/SingleInstance/Autostart
- Tray Menü dynamisch, Close-to-tray, Autoplay Preconditions, Mutex+IPC, Task Scheduler

### Note (Option C2)
- Refactor startup orchestration into a dedicated `StartupService`/`AppHost` (settings->logging->library->preconditions->tray/main window) to replace the interim C1 StartupWindow bootstrap.

## Milestone 7 — Logging UI
- Serilog daily files, Logs Seite (Liste+Viewer, Delete/Refresh/Open folder)

## Milestone 8 — Packaging
- GitHub Actions Release ZIP (Tag `v*`)
