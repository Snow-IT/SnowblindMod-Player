# Snowblind‑Mod Player — Architektur & Tech‑Entscheidungen (Option A)

Stand: 2026-01-24

Dieses Dokument ergänzt die funktionale Spezifikation um die festgelegten Architektur- und Technologieentscheidungen.

## Festgelegte Technologie
- **UI/Host:** WPF (.NET 8 oder neuer)
- **Architektur:** MVVM + Services, Clean-ish Layering
  - Projekte: `SnowblindModPlayer.App`, `.UI`, `.Core`, `.Infrastructure`, `.Tests`
- **Playback:** **LibVLC (VLCSharp)** in separatem `PlayerWindow`
- **Thumbnails:** Primär **VLC Snapshot** (Seek @ 5% der Videolänge)
  - **Queue:** max 1 Thumbnail-Job parallel
  - **Fallback:** wenn Dauer unbekannt/zu kurz → 1s oder 0s
  - **Robustheit:** Timeout/Retry; bei Fehlschlag Placeholder + Warnlog; Import gilt als erfolgreich
- **Tray:** **H.NotifyIcon.Wpf** (dynamisches Menü)
- **Autostart:** **Windows Task Scheduler** (LogonTrigger, optional Delay), Startparameter `--tray`
- **Single Instance:** Mutex + IPC (Named Pipe), Zweitstart fokussiert bestehende Instanz, kein erneutes Autoplay
- **Logging:** **Serilog** (tägliche Dateien `YYYY-MM-DD.log`) + optional UI/InMemory Sink
- **Datenhaltung (Hybrid):**
  - **Settings:** JSON (`%AppData%\SnowblindModPlayer\settings.json`)
  - **Library:** SQLite (`%AppData%\SnowblindModPlayer\library.db`) via `Microsoft.Data.Sqlite`
  - **Constraint:** `originalSourcePath` UNIQUE (Duplikatprüfung B1)
- **Packaging (Phase 1):** Portable ZIP via GitHub Actions (Tag `v*`)

## Leitprinzipien
- **Risk-first:** Playback/Fullscreen/Monitor/Thumbnail früh validieren.
- **Austauschbarkeit:** zentrale Interfaces (`IPlaybackService`, `IThumbnailService`, …)
- **Non-blocking UI:** Import/Copy/Thumbnail asynchron; UI bleibt responsiv.
