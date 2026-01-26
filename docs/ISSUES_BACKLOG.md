# Issues Backlog (Milestones M0–M8)

> Source: `docs/SPEC_FINAL.md`, `docs/ARCHITECTURE.md`, `docs/IMPLEMENTATION_PLAN.md`, `docs/TEST_CHECKLIST.md`, `docs/DATA_MODEL.md`.

## M0 – Foundations & Skeleton
- **Title:** M0 Epic – Foundations & Skeleton
- **Summary:** Bootstrap app skeleton (WPF/.NET 8), DI wiring, theme infrastructure, base MVVM + placeholder screens, LibVLC init spike, settings persistence baseline.
- **Acceptance Criteria:**
  - Solution builds; DI container composes Core/Infrastructure/UI/App layers.
  - LibVLC initialized without crashes on startup (no playback UI yet).
  - Theme switching Light/Dark via settings hook works; resources merged.
  - Settings JSON persisted to `%AppData%` path.
  - Placeholder pages for Videos/Logs/Settings load without errors.
- **Tests:**
  - Unit: settings path resolution, default media folder creation.
  - Smoke: launch app -> startup window closes -> main window shows; no unhandled exceptions.
- **Dependencies:** none.
- **Labels:** epic, milestone/M0

  - **Sub-issue:** M0-1 Baseline DI + startup flow
    - Summary: Wire ServiceCollection, database init hook, settings load on startup window.
    - Acceptance: Startup window closes after services ready; main window shows; DB init called.
    - Tests: Mock appdata path test; smoke start.
    - Dependencies: M0 Epic.
    - Labels: feature, area:bootstrap
  - **Sub-issue:** M0-2 Theme resource setup
    - Summary: Add Light/Dark resource dictionaries and switch via ThemeService.
    - Acceptance: Switching flag reloads theme without restart; default follows system.
    - Tests: Manual smoke toggle; unit for preference resolution.
    - Dependencies: M0 Epic.
    - Labels: feature, area:ui

## M1 – Playback Spike + Hotkeys
- **Title:** M1 Epic – Playback & Hotkeys
- **Summary:** Implement PlayerWindow with LibVLC playback, basic controls, required hotkeys/mouse actions.
- **Acceptance Criteria:**
  - PlayerWindow plays a selected local video; separate window.
  - Hotkeys: Space play/pause; Left/Right ±5s; Shift+Left/Right ±30s; Up/Down volume ±5%; M mute toggle; F11 fullscreen toggle; ESC fullscreen->windowed else stop+close; mouse click play/pause; double-click fullscreen; wheel volume.
  - Loop on/off supported.
  - Scaling option supports fill and aspect (setting placeholder ok if not yet UI-bound).
- **Tests:**
  - Manual: load sample video; verify hotkeys/mouse actions.
  - Unit: PlaybackService loop restart handler coverage (EndReached logic).
- **Dependencies:** M0.
- **Labels:** epic, milestone/M1

  - **Sub-issue:** M1-1 PlaybackService hotkey/loop support
    - Summary: Implement loop restart, position/volume tracking, scaling options placeholders.
    - Acceptance: EndReached restarts when loop on; exposes events.
    - Tests: Unit for loop restart path.
    - Dependencies: M1 Epic.
    - Labels: feature, area:playback
  - **Sub-issue:** M1-2 PlayerWindow hotkeys & OSD
    - Summary: Wire hotkeys/mouse controls, OSD feedback, volume/mute UI.
    - Acceptance: Hotkeys per spec; OSD shows status; no crashes.
    - Tests: Manual hotkey matrix.
    - Dependencies: M1 Epic.
    - Labels: feature, area:ui

## M2 – Multi-Monitor Selection + Fullscreen
- **Title:** M2 Epic – Multi-Monitor & Fullscreen UX
- **Summary:** Provide monitor selection UI (graphical layout), persist monitor choice, fullscreen respects selection, ESC behavior.
- **Acceptance Criteria:**
  - Settings store `SelectedMonitor`; reset if monitor unavailable at start.
  - Monitor selection view shows rectangles with numbers, clickable selection.
  - Fullscreen enters chosen monitor; ESC in fullscreen returns to windowed without stopping playback.
- **Tests:**
  - Manual: select secondary monitor; toggle fullscreen; unplug monitor -> selection resets.
  - Unit: monitor service returns null when missing.
- **Dependencies:** M1.
- **Labels:** epic, milestone/M2

  - **Sub-issue:** M2-1 MonitorService enhancement
    - Summary: Detect monitors, persist ID, handle unavailable monitor.
    - Acceptance: `GetSelectedMonitor` null when missing; selection saved.
    - Tests: Unit with mock monitor list.
    - Dependencies: M2 Epic.
    - Labels: feature, area:monitor
  - **Sub-issue:** M2-2 Monitor selection UI
    - Summary: WPF view to render monitor rectangles; selection persists to settings.
    - Acceptance: Click selects; shows number; reflects unavailable state.
    - Tests: Manual selection.
    - Dependencies: M2 Epic.
    - Labels: feature, area:ui

## M3 – Storage Hybrid + Cleanup
- **Title:** M3 Epic – Settings/DB Hybrid & Cleanup
- **Summary:** Complete settings defaults per spec/data_model, ensure DB schema and cleanup (E1) run at startup, unique constraints enforced.
- **Acceptance Criteria:**
  - Settings include keys/semantics per `DATA_MODEL.md` (e.g., `autostartEnabled`, `autoplayEnabled`, `startDelaySeconds`, `fullscreenOnStart`, `loopEnabled`, `muteEnabled`, `volumePercent`, `scalingMode`, `loggingLevel`, `trayCloseHintEnabled`, `mediaFolder`, `viewMode`, `sidebarCollapsed`, `languageMode`, `fixedLanguage`, `defaultVideoId`, `selectedMonitorId`).
  - SQLite schema stays on current `Media` table with CamelCase columns (`Id`, `DisplayName`, `OriginalSourcePath` UNIQUE, `StoredPath` UNIQUE, `DateAdded`, `ThumbnailPath` nullable). Cleanup removes missing files and thumbnails; default video reset if deleted.
  - Media folder migration prompt stub/flow recorded (if not fully implemented yet).
- **Tests:**
  - Unit: settings defaults + save/load roundtrip with key names above.
  - Integration: startup cleanup removes missing entries and thumbnails; default cleared if entry gone; schema matches `Media` table.
- **Dependencies:** M1.
- **Labels:** epic, milestone/M3

  - **Sub-issue:** M3-1 Settings coverage
    - Summary: Add missing keys + getters/setters + defaults; live-update hooks placeholder for log level/theme.
    - Acceptance: Settings file contains new keys after save; defaults match spec 7.1/data_model list.
    - Tests: Unit roundtrip.
    - Dependencies: M3 Epic.
    - Labels: feature, area:settings
  - **Sub-issue:** M3-2 DB cleanup & schema alignment
    - Summary: Ensure CleanupOrphanedEntries runs at startup and handles thumbnails; document retention of `Media` CamelCase schema and enforce uniques per implementation.
    - Acceptance: Missing files lead to entry deletion; default video reset when deleted; schema/table naming matches implementation and documentation.
    - Tests: Integration with temp files; schema validation.
    - Dependencies: M3 Epic.
    - Labels: feature, area:data

## M4 – Import/Remove + Thumbnail Queue
- **Title:** M4 Epic – Import & Thumbnails
- **Summary:** Implement import flow (validation, duplicate check B1, copy, collision C1), thumbnail queue (max 1 parallel, timeout+retry), removal deletes files and thumbnail, default reset.
- **Acceptance Criteria:**
  - Supported extensions whitelist per spec; unreadable/duplicates skipped with message.
  - File copy into media folder; unique naming when collision.
  - Thumbnails generated at 5% duration, fallback 1s/first frame; placeholder on failure; import succeeds even if thumbnail fails.
  - Remove confirms destructive delete; removes media + thumbnail; clears default if needed.
- **Tests:**
  - Unit: ImportService validation, collision naming, duplicate block.
  - Manual: import multiple files; thumbnail generation observed; removal deletes files.
- **Dependencies:** M3.
- **Labels:** epic, milestone/M4

  - **Sub-issue:** M4-1 Import UX and dialogs
    - Summary: Hook import command to OpenFileDialog; user messaging per outcomes.
    - Acceptance: Skips invalid/duplicates; shows success/warn dialogs.
    - Tests: Manual dialog flow.
    - Dependencies: M4 Epic.
    - Labels: feature, area:ui
  - **Sub-issue:** M4-2 Thumbnail queue robustness
    - Summary: Enforce single parallel job; timeout+retry; placeholder fallback.
    - Acceptance: Queue drains; failure doesn’t block imports.
    - Tests: Unit for queue retry/timeout; manual with corrupt file.
    - Dependencies: M4 Epic.
    - Labels: feature, area:playback

## M5 – Videos UI List/Tile + Toolbar
- **Title:** M5 Epic – Videos Page UX
- **Summary:** Implement list/tile view toggle, toolbar fixed, favorite badge, search filter, default video set/unset, selection behaviors.
- **Acceptance Criteria:**
  - View mode toggle (List/Tile) persists; default video marked (★) especially in Tile.
  - Toolbar fixed (not scrolling) with Add/Remove/Favorite buttons in title bar per spec.
  - Search filters display name case-insensitive.
  - Sidebar collapsible state persisted.
- **Tests:**
  - Manual: toggle view modes, set favorite, search filter, persist sidebar collapse.
- **Dependencies:** M4.
- **Labels:** epic, milestone/M5

  - **Sub-issue:** M5-1 UI layout for Videos page
    - Summary: Build list & tile DataTemplates, default badge, fixed toolbar.
    - Acceptance: Toolbar stays visible; tiles show thumbnail/name/default badge.
    - Tests: Manual UI verification.
    - Dependencies: M5 Epic.
    - Labels: feature, area:ui
  - **Sub-issue:** M5-2 Favorite/default handling
    - Summary: Set/unset default via toolbar; ensure only one default; update settings.
    - Acceptance: Default persists across restarts; badge updates immediately.
    - Tests: Unit for SetDefaultVideo; manual flows.
    - Dependencies: M5 Epic.
    - Labels: feature, area:data

## M6 – Tray + Autoplay + Single Instance + Autostart
- **Title:** M6 Epic – Tray/Autoplay/Instance Control
- **Summary:** Add H.NotifyIcon tray icon/menu, close-to-tray behavior, single-instance with Mutex+NamedPipe focus, autostart via Task Scheduler, autoplay logic with delay/monitor preconditions.
- **Acceptance Criteria:**
  - Tray menu items: show main window, play default, play specific (submenu sorted with default first ★), stop playback, exit app.
  - Closing main window hides to tray; exit only via tray menu.
  - Single instance: second launch signals first to focus main/tray; no autoplay rerun.
  - Autostart creates/removes scheduled task on setting toggle; app starts with `--tray` on autostart.
  - Autoplay: if enabled and preconditions met (default video exists, monitor available), starts after delay in tray; otherwise shows main with reason.
- **Tests:**
  - Manual: second instance focus; tray menu actions; autoplay with delay; autostart task creation.
  - Unit: IPC message handler; settings flag roundtrip.
- **Dependencies:** M5.
- **Labels:** epic, milestone/M6

  - **Sub-issue:** M6-1 Single-instance + IPC
    - Summary: Mutex + NamedPipe for second-start notification; focus existing; block autoplay on second start.
    - Acceptance: Second start exits after notifying; main window focused.
    - Tests: Manual dual launch; unit for handler.
    - Dependencies: M6 Epic.
    - Labels: feature, area:platform
  - **Sub-issue:** M6-2 Tray integration + close-to-tray
    - Summary: Add H.NotifyIcon, tray menu with required items, close hides to tray; exit only via tray.
    - Acceptance: X shows popup (setting-controlled), app stays running; tray menu works.
    - Tests: Manual tray interactions.
    - Dependencies: M6 Epic.
    - Labels: feature, area:ui
  - **Sub-issue:** M6-3 Autostart + autoplay logic
    - Summary: Task Scheduler LogonTrigger with `--tray`; autoplay delay & precondition check.
    - Acceptance: Task created/removed; autoplay starts when eligible; shows reason when not.
    - Tests: Manual logon simulation; unit for precondition function.
    - Dependencies: M6 Epic.
    - Labels: feature, area:platform

## M7 – Logs UI + File Viewer
- **Title:** M7 Epic – Logging UI & Level Control
- **Summary:** Integrate Serilog rolling files, live level changes, logs page with file list + viewer, actions (Refresh, Delete with confirm, Open folder), two scrollable panes.
- **Acceptance Criteria:**
  - Serilog writes `YYYY-MM-DD.log` in log folder; level default Warn; runtime level change via settings.
  - Logs view shows file list (left, scrollable) and content (right, scrollable, monospaced).
  - Buttons: Refresh (always active), Delete (with selection confirm), Open folder (always active).
- **Tests:**
  - Unit: logging configuration yields expected path/name.
  - Manual: level change affects new entries; delete removes file; refresh reloads list.
- **Dependencies:** M6.
- **Labels:** epic, milestone/M7

  - **Sub-issue:** M7-1 Serilog configuration + level switch
    - Summary: Configure rolling file sink; expose setting to change level at runtime.
    - Acceptance: Level change applies without restart; files roll daily.
    - Tests: Unit/integ writing sample log; manual level toggle.
    - Dependencies: M7 Epic.
    - Labels: feature, area:logging
  - **Sub-issue:** M7-2 Logs UI and actions
    - Summary: Build two-pane logs page with list/content, actions, confirmations.
    - Acceptance: Scroll independence; actions follow spec; monospaced view.
    - Tests: Manual UI flow.
    - Dependencies: M7 Epic.
    - Labels: feature, area:ui

## M8 – Packaging (Portable ZIP via GitHub Actions)
- **Title:** M8 Epic – Portable ZIP Packaging
- **Summary:** CI workflow builds .NET 8 WPF app, produces portable ZIP on tag `v*`, includes required assets.
- **Acceptance Criteria:**
  - GitHub Actions workflow triggered on tags `v*` builds solution, publishes trimmed output, zips artifacts.
  - Release asset contains all binaries/resources including VLC native deps and tray icon.
  - Version info derived from tag.
- **Tests:**
  - CI run on sample tag; artifact present and runnable locally.
- **Dependencies:** M6+ (tray/autoplay) and M7 (logging) ideally completed before packaging.
- **Labels:** epic, milestone/M8

  - **Sub-issue:** M8-1 CI workflow for portable ZIP
    - Summary: Add GH Actions YAML to publish self-contained or portable ZIP; include VLC native libs.
    - Acceptance: Workflow succeeds on tag; ZIP downloadable.
    - Tests: CI run.
    - Dependencies: M8 Epic.
    - Labels: feature, area:ci
