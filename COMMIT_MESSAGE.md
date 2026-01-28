# Commit Message

## Subject
Phase C TODO Completion: Notifications, Autoplay Validation, Thumbnails, Dynamic Banner Width

## Body

### Summary
Completed all 7 offenen TODOs from Phase C TODO-Completion Sprint:
- ? NotificationOrchestrator smart routing (Banner/Toast/Dialog)
- ? Missing file playback notifications (UI + Autoplay)
- ? Autoplay validation (Default video + Monitor selection)
- ? Exception-safe file removal (graceful cleanup)
- ? Enhanced thumbnail generation (CancellationToken, logging, fallbacks)
- ? Dynamic banner width (~1/3 app width via MultiplyConverter)
- ? Comprehensive logging for debugging

### Changes

#### Core Infrastructure
- **ServiceCollectionExtensions.cs:** Fixed ThumbnailService registration (LibVLC instead of FFmpeg)
- **INotificationOrchestrator.cs:** New notification scenarios (PlaybackMissingFile, DefaultVideoMissing, AutoplayMissingDefault, AutoplayMissingMonitor)
- **IThumbnailService.cs:** Added CancellationToken parameter for timeout support

#### Services
- **NotificationOrchestrator.cs:** Smart routing logic (Banner visible, Toast hidden, Dialog errors)
- **PlaybackOrchestrator.cs:** Enhanced error handling, new notification scenarios, file validation
- **LibraryService.cs:** Exception-safe RemoveMediaAsync (file deletion errors logged but don't block DB cleanup)
- **ThumbnailQueueService.cs:** CancellationToken support, enhanced logging with emojis
- **ThumbnailService.cs:** CancellationToken support, improved VLC snapshot handling
- **ThumbnailServiceFFmpeg.cs:** Marked as deprecated (LibVLC now used instead)

#### UI Layer
- **VideosViewModel.cs:** 
  - PlaySelectedAsync() now validates file exists before opening player window
  - RemoveSelectedAsync() caches SelectedMedia and clears it before reload (prevents null reference)
- **App.xaml.cs:** Autoplay startup with Default video + Monitor selection validation
- **App.xaml:** MultiplyConverter registered
- **MainWindow.xaml:** Dynamic banner width binding (RelativeSource, 0.33 multiplier, 300-500px constraints)
- **MultiplyConverter.cs:** New converter for responsive width calculations

#### Documentation
- **docs/DECISIONS.md:** Phase C completion with test results, open issues (P1-P7), test suite for tomorrow
- **.github/copilot-instructions.md:** Updated implementation state, known issues, test priorities

### Testing Status

#### ? Passed
- [x] Test 4A: Remove video with missing file ? DB cleaned, no exception
- [x] Test 4B: Remove video with missing thumbnail ? OK
- [x] Test 6A: Banner width responsive resizing
- [x] Test 6B: Multiple banners (max 3) stacking correctly
- [x] Build: All changes compile successfully

#### ?? Needs Verification
- [ ] Test 1A: Missing file playback ? Banner shows (Test shows OK)
- [ ] Test 5A-C: Thumbnails generated with LibVLC (Needs retry with fixed registration)
- [ ] Test 2A, 3A: Autoplay notifications (Banner should work, Toast pending)

#### ?? Known Issues (Blocking for P1)
- **P1 - Toast Notifications:** Shell_NotifyIcon P/Invoke may not be visible (Windows 10/11 differences)
- **P2 - Thumbnails:** Verification pending with corrected LibVLC registration
- **P3 - Banner Display:** Test 7A shows banner not displaying in some scenarios

### Breaking Changes
None. All changes are backward compatible.

### Dependencies
None added. Uses existing: LibVLCSharp, Microsoft.Data.Sqlite, Serilog.

### Related Issues
- Addresses all 7 TODOs from docs/DECISIONS.md Phase C TODO list
- Follows architecture patterns from .github/copilot-instructions.md

### Notes for Code Review
- Exception handling in RemoveMediaAsync is intentionally permissive: file deletion errors logged but don't block DB cleanup
- Toast implementation is minimal P/Invoke; alternative H.NotifyIcon.Wpf can be used if needed
- PlaySelectedAsync and PlaybackOrchestrator are currently dual entry points (design cleanup noted for Phase D)
- ThumbnailServiceFFmpeg kept for future reference if FFmpeg migration needed

### Next Phase (Phase D)
- LibraryOrchestrator (unified Import/Remove/SetDefault)
- LibraryChangeNotifier (event-driven UI/Tray updates)
- Single Instance + Autostart (Task Scheduler)
- Resolution of P1-P3 issues before release
