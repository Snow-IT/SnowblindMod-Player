# Decisions / Annahmen

Nutze dieses Dokument, um Annahmen festzuhalten, falls bei der Umsetzung Details fehlen oder unklar sind.

- 2026-01-26: Backlog in `docs/ISSUES_BACKLOG.md` basiert auf `docs/SPEC_FINAL.md`, `docs/ARCHITECTURE.md`, `docs/IMPLEMENTATION_PLAN.md`, `docs/TEST_CHECKLIST.md`, `docs/DATA_MODEL.md`.
- 2026-01-26: Schema-Entscheidung: Beibehaltung der bestehenden SQLite-Tabelle `Media` (CamelCase-Spalten `Id`, `DisplayName`, `OriginalSourcePath` UNIQUE, `StoredPath` UNIQUE, `DateAdded`, `ThumbnailPath`) zur Vermeidung von Migration; `DATA_MODEL.md` und Backlog (M3-2) entsprechend angepasst.
