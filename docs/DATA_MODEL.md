# Datenmodell & Datenhaltung (Hybrid)

## Settings (JSON)
Pfad: `%AppData%\SnowblindModPlayer\settings.json`

Empfohlene Keys (Auszug):
- `autostartEnabled` (bool)
- `autoplayEnabled` (bool)
- `startDelaySeconds` (int)
- `fullscreenOnStart` (bool)
- `loopEnabled` (bool)
- `muteEnabled` (bool)
- `volumePercent` (int 0..100)
- `scalingMode` ("Fill" | "KeepAspect")
- `loggingLevel` ("Error"|"Warn"|"Info"|"Debug")
- `trayCloseHintEnabled` (bool)
- `mediaFolder` (string)
- `viewMode` ("List"|"Tile")
- `sidebarCollapsed` (bool)
- `languageMode` ("System"|"Fixed")
- `fixedLanguage` (e.g. "de-DE")
- `defaultVideoId` (string|null)
- `selectedMonitorId` (string|null)

## Library (SQLite)
Pfad: `%AppData%\SnowblindModPlayer\library.db`

### Tabelle `media_items`
- `id` TEXT PRIMARY KEY
- `display_name` TEXT NOT NULL
- `original_source_path` TEXT NOT NULL UNIQUE
- `stored_path` TEXT NOT NULL
- `date_added` TEXT NOT NULL (ISO)
- `thumbnail_path` TEXT NOT NULL

### Startup Cleanup (E1)
- Items ohne existierenden `stored_path` werden beim Start gelöscht (inkl. Thumbnail).
