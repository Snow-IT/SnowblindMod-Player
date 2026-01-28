# Phase C + D: Comprehensive Test Checklist

## ?? Core Functionality Tests

### 1. Tray Integration
- [ ] Tray icon visible in system tray (transparent background)
- [ ] Tray icon responds to double-click ? shows main window
- [ ] Right-click context menu appears
- [ ] Menu items: Show, Play Default, Play Video, Stop, Exit

### 2. Playback (PlaybackOrchestrator)
- [ ] Play video from Tray ? Play Video ? [valid video]
  - [ ] PlayerWindow opens on selected monitor
  - [ ] Video plays immediately
  - [ ] Correct volume/fullscreen/loop settings applied
- [ ] Play missing file from Tray ? Error toast appears
- [ ] Play valid file from UI (Videos tab) ? works correctly
- [ ] Play missing file from UI ? Error banner appears (MainWindow visible)
- [ ] Default video auto-starts on Autoplay (with delay)

### 3. Notifications (All Scenarios)

#### Import
- [ ] Import 1 video ? Success toast "Imported 1 video(s)"
- [ ] Import 3 videos ? Success toast "Imported 3 video(s)"
- [ ] Import invalid files (duplicates) ? Warning banner "No videos were imported"
- [ ] Import with bad file ? Error banner "Import failed: [reason]"
- [ ] Imported videos appear in VideosView immediately

#### Remove
- [ ] Remove video ? Confirmation dialog appears
- [ ] Click Yes ? Success banner "Removed: [name]"
- [ ] Click No ? nothing happens
- [ ] Remove missing file ? Success banner (file cleanup works)

#### Default Video
- [ ] Set as default ? Success banner "Default set: [name]"
- [ ] Badge "?" appears next to default in list
- [ ] Tray menu shows "[DEFAULT]" prefix on default video

#### Playback Errors
- [ ] Play video with missing file ? Error banner "Video file not found: [name]"
- [ ] Play when no default set ? Warning banner "No default video set"
- [ ] Play when monitor missing ? Warning banner "No monitor selected - playback skipped"
- [ ] PlayerWindow error ? Error banner "Player window not available"

#### Autoplay
- [ ] Autoplay enabled + valid default + valid monitor ? Toast "Playing: [name]" + video starts
- [ ] Autoplay enabled + NO default ? Warning toast "No default video set - autoplay skipped"
- [ ] Autoplay enabled + monitor removed from settings ? Warning toast "No monitor selected - autoplay skipped"
- [ ] Autoplay delay working (wait X seconds before starting)

#### Settings
- [ ] Change theme ? Success banner "Settings saved"
- [ ] Toggle loop ? Success banner "Settings saved"
- [ ] Toggle fullscreen ? Success banner "Settings saved"
- [ ] Change volume ? Success banner "Settings saved"
- [ ] Change autoplay delay ? Success banner "Settings saved"
- [ ] Select different monitor ? Success banner "Display set to: [name]"

#### Tray Minimize
- [ ] Click Close button (MainWindow) ? Info toast "Application minimized to tray"
- [ ] App stays running in tray
- [ ] Can restore via tray double-click

### 4. Theme Integration
- [ ] Switch to Light theme ? Success banner appears with green color
- [ ] Switch to Dark theme ? Success banner appears with green color
- [ ] Switch to System theme ? respects OS setting
- [ ] Error banners show red in both themes
- [ ] Warning banners show orange in both themes
- [ ] Info banners show blue in both themes

### 5. Banner Animations
- [ ] One banner appears ? shows 5s then fades out + slides up
- [ ] Two banners appear together ? second pushes up after first auto-dismisses
- [ ] Three banners visible ? fourth oldest removed when max 3 reached
- [ ] Transitions are smooth (no jarring jumps)

### 6. Toast Window (Tray Notifications)
- [ ] Toast appears bottom-right corner
- [ ] Toast shows: App icon + "Snowblind-Mod Player" header
- [ ] Toast shows notification type icon (? Error, ?? Warning, ? Success, ?? Info)
- [ ] Toast shows title + message readable
- [ ] Toast auto-dismisses after 6 seconds
- [ ] Multiple toasts appear sequentially (not stacked)

### 7. Tray Video Menu
- [ ] Default video listed first with "[DEFAULT]" prefix
- [ ] Other videos sorted alphabetically
- [ ] After import, new video appears in Tray menu (without manual refresh)
- [ ] After remove, video disappears from Tray menu

### 8. LibraryOrchestrator (Foundation)
- [ ] LibraryOrchestrator registered in DI ?
- [ ] Builds without errors ?
- [ ] Ready for VideosViewModel wiring (next sprint)

---

## ?? Edge Cases & Stress Tests

### File System
- [ ] Import same file twice ? 2nd shows warning (duplicate)
- [ ] Delete video file manually ? Next remove still works gracefully
- [ ] Clear monitor selection from settings ? Autoplay skips with notification
- [ ] Change monitor in settings ? Success banner "Display set to: [monitor]"

### Rapid Changes
- [ ] Import 5 videos rapidly ? All show success toasts/banners
- [ ] Remove 3 videos rapidly ? All succeed
- [ ] Change settings repeatedly ? Notifications appear for each

### Recovery
- [ ] Corrupt default video setting ? App recovers gracefully
- [ ] Missing monitor ? Autoplay skips, manual play also skips
- [ ] Delete all videos ? UI empty but responsive

---

## ?? Detailed Test Scenario Examples

### Scenario 1: Full Import ? Play ? Remove Flow
1. Videos tab ? Import ? select 2 videos
2. Check: Success banner "Imported 2 video(s)"
3. Check: Videos appear in list
4. Check: Tray menu updated with new videos
5. Click one ? PlayerWindow opens + plays
6. Close ? back to Videos
7. Select ? Remove
8. Confirm ? Success banner "Removed: [name]"
9. Check: Tray menu updated (video gone)

### Scenario 2: Autoplay with Delay
1. Settings ? Autoplay ON + Delay 3s + select monitor + set default
2. Close app (or restart)
3. Check: Toast "Application minimized to tray" appears
4. Wait 3s ? Toast "Playing: [name]" appears
5. PlayerWindow opens + video plays

### Scenario 3: Theme Switch + Import
1. Settings ? Switch theme to Dark
2. Videos tab ? Import 1 video
3. Check: Banner shows with dark-theme green color
4. Switch theme to Light
5. Import another video
6. Check: Banner shows with light-theme green color

### Scenario 4: Missing File Recovery
1. Import 1 video
2. Navigate to AppData/SnowblindModPlayer/media/ ? delete the video file
3. UI still shows video (but with missing icon/state eventually)
4. Try to play ? Error banner "Video file not found"
5. Remove ? Success banner "Removed: [name]"
6. Check: DB cleaned up

---

## ? Sign-Off Criteria

- [ ] All 4 notification types working (Info/Success/Warning/Error)
- [ ] All scenarios from matrix trigger correct notification
- [ ] Autoplay validates default + monitor before starting
- [ ] Tray menu updates on import/remove (even if LibraryOrchestrator not wired to VideosViewModel yet)
- [ ] Theme colors render correctly in Light/Dark
- [ ] No unhandled exceptions in debug output
- [ ] Build compiles cleanly

---

## ?? Known Limitations (Acceptable for now)

- VideosViewModel still calls Import/Remove directly (not via LibraryOrchestrator) — will be fixed in next sprint
- Tray video menu may not auto-update if import via UI (workaround: refresh context menu) — will be fixed with LibraryOrchestrator wiring
- H.NotifyIcon P/Invoke toasts not visible (Windows 10/11 limitation) — using custom popup instead (working)

---

## ?? Test Execution Notes

**Duration:** ~30-45 minutes for full run  
**Recommended Order:** Core ? Notifications ? Theme ? Edge Cases ? Scenarios  
**Environment:** Windows 10/11, test with different monitors if possible

After each test, check:
- ? No exceptions in Debug Output
- ? Database still valid (no corruption)
- ? App remains responsive
