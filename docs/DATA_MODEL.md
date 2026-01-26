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

### Tabelle `Media`
- `Id` TEXT PRIMARY KEY
- `DisplayName` TEXT NOT NULL
- `OriginalSourcePath` TEXT NOT NULL UNIQUE
- `StoredPath` TEXT NOT NULL UNIQUE
- `DateAdded` TEXT NOT NULL (ISO)
- `ThumbnailPath` TEXT

### Startup Cleanup (E1)
- Items ohne existierenden `StoredPath` werden beim Start gelöscht (inkl. Thumbnail, DefaultVideo-Reset).
