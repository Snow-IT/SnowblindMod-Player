using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Services;

namespace SnowblindModPlayer.Services;

/// <summary>
/// Unified video playback orchestrator.
/// Single entry point for all "play video" scenarios (tray, UI, autoplay).
/// Handles: opening PlayerWindow, applying settings, starting playback.
/// </summary>
public class PlaybackOrchestrator
{
    private readonly ILibraryService _libraryService;
    private readonly IPlaybackService _playbackService;
    private readonly ISettingsService _settingsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationOrchestrator _notifier;
    private readonly ILoggingService _logger;

    public PlaybackOrchestrator(
        ILibraryService libraryService,
        IPlaybackService playbackService,
        ISettingsService settingsService,
        IServiceProvider serviceProvider,
        INotificationOrchestrator notifier,
        ILoggingService logger)
    {
        _libraryService = libraryService;
        _playbackService = playbackService;
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        _notifier = notifier;
        _logger = logger;
    }

    /// <summary>
    /// Unified entry point: Play video by ID.
    /// Opens PlayerWindow with all settings applied (monitor, fullscreen, volume, loop, etc.)
    /// </summary>
    public async Task PlayVideoAsync(string videoId)
    {
        try
        {
            var video = await _libraryService.GetMediaByIdAsync(videoId);
            if (video == null)
            {
                _logger.Log(LogLevel.Warn, "Playback", $"Video not found: {videoId}");
                await _notifier.NotifyErrorAsync(
                    "Video not found in library",
                    null,
                    NotificationScenario.PlaybackError);
                return;
            }

            // Check if file exists
            if (string.IsNullOrWhiteSpace(video.StoredPath) || !File.Exists(video.StoredPath))
            {
                _logger.Log(LogLevel.Warn, "Playback", $"Missing file: {video.DisplayName}");
                await _notifier.NotifyErrorAsync(
                    $"Video file not found: {video.DisplayName}",
                    null,
                    NotificationScenario.PlaybackMissingFile);
                return;
            }

            var monitorId = _settingsService.GetSelectedMonitorId();
            if (string.IsNullOrWhiteSpace(monitorId))
            {
                _logger.Log(LogLevel.Warn, "Playback", "No monitor selected - playback skipped");
                await _notifier.NotifyAsync(
                    "No monitor selected - playback skipped", 
                    NotificationScenario.PlaybackError, 
                    NotificationType.Warning);
                return;
            }

            // Marshal UI work to the dispatcher (Autoplay runs on background thread)
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var playerWindow = _serviceProvider.GetService(typeof(PlayerWindow)) as PlayerWindow;
                if (playerWindow == null)
                {
                    _logger.Log(LogLevel.Error, "Playback", "Player window not available");
                    await _notifier.NotifyErrorAsync(
                        "Player window not available",
                        null,
                        NotificationScenario.PlaybackError);
                    return;
                }

                // Ensure window is shown and positioned on selected monitor (handles in Loaded event)
                playerWindow.Show();

                // Apply all settings before playback
                await ApplyPlaybackSettingsAsync();

                // Start playback
                await _playbackService.PlayAsync(video.StoredPath);

                System.Diagnostics.Debug.WriteLine($"? Playback started: {video.DisplayName}");
                _logger.Log(LogLevel.Info, "Playback", $"Playback started: {video.DisplayName}");
            }, DispatcherPriority.Normal);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, "Playback", $"Playback failed: {ex.Message}", ex);
            await _notifier.NotifyErrorAsync(
                $"Playback failed: {ex.Message}",
                ex,
                NotificationScenario.PlaybackError);
        }
    }

    /// <summary>
    /// Unified entry point: Play default video.
    /// Validates that default video exists and file is accessible.
    /// </summary>
    public async Task PlayDefaultVideoAsync()
    {
        try
        {
            var defaultVideo = await _libraryService.GetDefaultVideoAsync();
            if (defaultVideo == null)
            {
                _logger.Log(LogLevel.Warn, "Playback", "No default video set");
                await _notifier.NotifyAsync(
                    "No default video set", 
                    NotificationScenario.AutoplayMissingDefault, 
                    NotificationType.Warning);
                return;
            }

            // Validate file exists
            if (string.IsNullOrWhiteSpace(defaultVideo.StoredPath) || !File.Exists(defaultVideo.StoredPath))
            {
                _logger.Log(LogLevel.Warn, "Playback", $"Default file missing: {defaultVideo.DisplayName}");
                await _notifier.NotifyErrorAsync(
                    $"Default video file not found: {defaultVideo.DisplayName}",
                    null,
                    NotificationScenario.PlaybackMissingFile);
                return;
            }

            await PlayVideoAsync(defaultVideo.Id);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, "Playback", $"PlayDefault failed: {ex.Message}", ex);
            await _notifier.NotifyErrorAsync(
                $"PlayDefault failed: {ex.Message}",
                ex,
                NotificationScenario.PlaybackError);
        }
    }

    /// <summary>
    /// Apply all playback settings from Settings service.
    /// This is called before every playback to ensure consistent state.
    /// </summary>
    private async Task ApplyPlaybackSettingsAsync()
    {
        try
        {
            // Volume
            var volume = _settingsService.Get("Volume", 70);
            await _playbackService.SetVolumeAsync(volume);

            // Mute
            var muted = _settingsService.Get("Muted", false);
            await _playbackService.SetMuteAsync(muted);

            // Loop (stored as internal flag in PlaybackService)
            var loopEnabled = _settingsService.Get("LoopEnabled", false);
            System.Diagnostics.Debug.WriteLine($"? Loop enabled: {loopEnabled}");

            System.Diagnostics.Debug.WriteLine($"? Playback settings applied: Volume={volume}%, Muted={muted}, Loop={loopEnabled}");
            _logger.Log(LogLevel.Debug, "Playback", $"Settings applied: Volume={volume} Muted={muted} Loop={loopEnabled}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Failed to apply playback settings: {ex.Message}");
            _logger.Log(LogLevel.Error, "Playback", $"Apply settings failed: {ex.Message}", ex);
        }
    }
}
