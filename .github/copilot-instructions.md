# Snowblind-Mod Player — Repo Instructions (MUST FOLLOW)

You are implementing the Windows desktop app "Snowblind-Mod Player".  
Follow docs/SPEC_FINAL.md strictly.

## Fixed Tech Decisions (do not change)
- UI: WPF (.NET 8+), MVVM + Services; implement an app-wide design system with global WPF styles/templates consistent across Light/Dark themes, where only colors change, not layout/controls.
- Playback: LibVLC via VLCSharp, separate PlayerWindow
- Thumbnails: VLC snapshot at 5% duration; fallback 1s/0s; async queue max 1 parallel; timeout+retry; on failure placeholder+warn; import still succeeds
- Storage: Hybrid (Settings JSON, Library SQLite via Microsoft.Data.Sqlite); originalSourcePath UNIQUE
- Tray: H.NotifyIcon.Wpf; Close-to-tray; exit only via tray
- Autostart: Windows Task Scheduler LogonTrigger; app starts with --tray
- Single instance: Mutex + IPC (NamedPipe); second start focuses existing, no autoplay
- Logging: Serilog daily rolling file (YYYY-MM-DD.log), level live changeable
- Packaging: Portable ZIP via GitHub Actions (tag v*)

## Agent behavior
- Implement directly (agent style). Make changes in small batches.
- After edits: ensure solution builds; add minimal tests when cheap.
- If ambiguous: document assumptions in docs/DECISIONS.md (do not change requirements).

## Implementation order (Risk-first)
1) Playback spike + hotkeys
2) Multi-monitor selection + fullscreen
3) Storage hybrid + cleanup
4) Import/remove + thumbnail queue
5) Videos UI list/tile + toolbar
6) Tray + autoplay + single instance + autostart
7) Logs UI + file viewer
8) GitHub Actions portable ZIP
