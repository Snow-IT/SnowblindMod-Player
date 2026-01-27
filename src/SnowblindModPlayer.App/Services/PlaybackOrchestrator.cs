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

    public PlaybackOrchestrator(
        ILibraryService libraryService,
        IPlaybackService playbackService,
        ISettingsService settingsService,
        IServiceProvider serviceProvider,
        INotificationOrchestrator notifier)
    {
        _libraryService = libraryService;
        _playbackService = playbackService;
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        _notifier = notifier;
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
                await _notifier.NotifyAsync("Video not found", NotificationScenario.PlaybackError, NotificationType.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(video.StoredPath) || !File.Exists(video.StoredPath))
            {
                await _notifier.NotifyAsync("Video file missing", NotificationScenario.PlaybackError, NotificationType.Error);
                return;
            }

            var monitorId = _settingsService.GetSelectedMonitorId();
            if (string.IsNullOrWhiteSpace(monitorId))
            {
                await _notifier.NotifyAsync("No monitor selected - skipping playback", NotificationScenario.PlaybackError, NotificationType.Warning);
                return;
            }

            // Marshal UI work to the dispatcher (Autoplay runs on background thread)
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var playerWindow = _serviceProvider.GetService(typeof(PlayerWindow)) as PlayerWindow;
                if (playerWindow == null)
                {
                    await _notifier.NotifyAsync("Player window not available", NotificationScenario.PlaybackError, NotificationType.Error);
                    return;
                }

                // Ensure window is shown and positioned on selected monitor (handles in Loaded event)
                playerWindow.Show();

                // Apply all settings before playback
                await ApplyPlaybackSettingsAsync();

                // Start playback
                await _playbackService.PlayAsync(video.StoredPath);

                System.Diagnostics.Debug.WriteLine($"? Playback started: {video.DisplayName}");
            }, DispatcherPriority.Normal);
        }
        catch (Exception ex)
        {
            await _notifier.NotifyAsync($"Playback failed: {ex.Message}", NotificationScenario.PlaybackError, NotificationType.Error);
        }
    }

    /// <summary>
    /// Unified entry point: Play default video.
    /// </summary>
    public async Task PlayDefaultVideoAsync()
    {
        try
        {
            var defaultVideo = await _libraryService.GetDefaultVideoAsync();
            if (defaultVideo == null)
            {
                await _notifier.NotifyAsync("No default video set", NotificationScenario.PlaybackError, NotificationType.Warning);
                return;
            }

            await PlayVideoAsync(defaultVideo.Id);
        }
        catch (Exception ex)
        {
            await _notifier.NotifyAsync($"PlayDefault failed: {ex.Message}", NotificationScenario.PlaybackError, NotificationType.Error);
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Failed to apply playback settings: {ex.Message}");
        }
    }
}
