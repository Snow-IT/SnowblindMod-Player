# Test- & Risiko-Checkliste (MVP)

## Playback
- Hotkeys: Space, Left/Right ±5s, Shift±30s, Up/Down Volume, M, F11, ESC Verhalten
- 3A: neues Video während Playback
- Fehleroverlay + Logging

## Multi‑Monitor
- Auswahl speichern/restore
- Vollbild auf Zielmonitor
- Monitor fehlt → Reset + Autoplay blockiert

## Import/Thumbnails
- Batch Import ohne UI Freeze
- B1 Duplikate verhindern
- C1 Name collision
- Thumbnail 5% + fallback, JPG 320px
- Thumbnail fail → Placeholder + Warnlog

## Tray/Autoplay/Single Instance
- X → Tray
- Tray Menü dynamisch + sortiert
- Autoplay Preconditions
- Zweitstart → Fokus, kein Autoplay

## Storage
- Settings persistieren, live updates
- SQLite konsistent (Kill-Test)
- E1 Cleanup
