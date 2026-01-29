using System;
using System.Windows.Controls;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Services;
using SnowblindModPlayer.Services;
using SnowblindModPlayer.UI.ViewModels;

namespace SnowblindModPlayer.Views;

public partial class SettingsView : UserControl
{
    private readonly ISettingsService _settingsService;
    private readonly INotificationOrchestrator _notifier;
    private readonly ILoggingService _logger;
    private readonly IAutostartService _autostartService;
    private TextBox? _autoplayDelayTextBox;

    public SettingsView(MonitorSelectionViewModel monitorSelectionViewModel, ISettingsService settingsService, INotificationOrchestrator notifier, ILoggingService logger, IAutostartService autostartService)
    {
        _settingsService = settingsService;
        _notifier = notifier;
        _logger = logger;
        _autostartService = autostartService;

        InitializeComponent();
        
        // Embed the MonitorSelectionView
        var monitorView = new MonitorSelectionView(monitorSelectionViewModel);
        MonitorSelectionHost.Content = monitorView;

        // Theme
        ThemePreferenceComboBox.ItemsSource = new[] { "System", "Light", "Dark" };
        ThemePreferenceComboBox.SelectedItem = _settingsService.GetThemePreference();
        ThemePreferenceComboBox.SelectionChanged += ThemePreferenceComboBox_SelectionChanged;

        // Playback settings
        LoopEnabledCheckBox.IsChecked = _settingsService.GetLoopEnabled();
        LoopEnabledCheckBox.Checked += (s, e) => OnSettingsChanged(() => _settingsService.SetLoopEnabled(true));
        LoopEnabledCheckBox.Unchecked += (s, e) => OnSettingsChanged(() => _settingsService.SetLoopEnabled(false));

        FullscreenOnStartCheckBox.IsChecked = _settingsService.GetFullscreenOnStart();
        FullscreenOnStartCheckBox.Checked += (s, e) => OnSettingsChanged(() => _settingsService.SetFullscreenOnStart(true));
        FullscreenOnStartCheckBox.Unchecked += (s, e) => OnSettingsChanged(() => _settingsService.SetFullscreenOnStart(false));

        MuteOnStartupCheckBox.IsChecked = _settingsService.GetMuted();
        MuteOnStartupCheckBox.Checked += (s, e) => OnSettingsChanged(() => _settingsService.SetMuted(true));
        MuteOnStartupCheckBox.Unchecked += (s, e) => OnSettingsChanged(() => _settingsService.SetMuted(false));

        VolumeSlider.Value = _settingsService.GetVolume();
        VolumeLabel.Text = $"{_settingsService.GetVolume()}%";
        VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;

        ScalingModeComboBox.ItemsSource = new[] { "Fill", "KeepAspect" };
        ScalingModeComboBox.SelectedItem = _settingsService.GetScalingMode();
        ScalingModeComboBox.SelectionChanged += ScalingModeComboBox_SelectionChanged;

        MinimizeToTrayOnStartupCheckBox.IsChecked = _settingsService.GetMinimizeToTrayOnStartup();
        MinimizeToTrayOnStartupCheckBox.Checked += (s, e) => OnSettingsChanged(() => _settingsService.SetMinimizeToTrayOnStartup(true));
        MinimizeToTrayOnStartupCheckBox.Unchecked += (s, e) => OnSettingsChanged(() => _settingsService.SetMinimizeToTrayOnStartup(false));

        AutostartEnabledCheckBox.IsChecked = _settingsService.GetAutostartEnabled();
        AutostartEnabledCheckBox.Checked += async (s, e) => await SetAutostartAsync(true);
        AutostartEnabledCheckBox.Unchecked += async (s, e) => await SetAutostartAsync(false);

        var autoplayEnabledCheckBox = (CheckBox)FindName("AutoplayEnabledCheckBox");
        if (autoplayEnabledCheckBox != null)
        {
            autoplayEnabledCheckBox.IsChecked = _settingsService.GetAutoplayEnabled();
            autoplayEnabledCheckBox.Checked += (s, e) => OnSettingsChanged(() => _settingsService.SetAutoplayEnabled(true));
            autoplayEnabledCheckBox.Unchecked += (s, e) => OnSettingsChanged(() => _settingsService.SetAutoplayEnabled(false));
        }

        var autoplayDelayTextBox = (TextBox)FindName("AutoplayDelayTextBox");
        if (autoplayDelayTextBox != null)
        {
            _autoplayDelayTextBox = autoplayDelayTextBox;
            _autoplayDelayTextBox.Text = _settingsService.GetAutoplayDelaySeconds().ToString();
            _autoplayDelayTextBox.LostFocus += (s, e) => ApplyAutoplayDelay();
        }

        // Log Level
        var logLevelComboBox = (ComboBox)FindName("LogLevelComboBox");
        if (logLevelComboBox != null)
        {
            logLevelComboBox.ItemsSource = new[] { "Debug", "Information", "Warning", "Error", "Critical" };
            logLevelComboBox.SelectedItem = _settingsService.Get("LogLevel", "Information");
            logLevelComboBox.SelectionChanged += (s, e) =>
            {
                if (logLevelComboBox.SelectedItem is string level)
                {
                    OnSettingsChanged(() => _settingsService.Set("LogLevel", level));
                    System.Diagnostics.Debug.WriteLine($"Log level changed to: {level}");
                }
            };
        }

        _settingsService.RegisterLiveUpdate<string>("ThemePreference", pref =>
        {
            Dispatcher.Invoke(() =>
            {
                if (ThemePreferenceComboBox.SelectedItem as string != pref)
                    ThemePreferenceComboBox.SelectedItem = pref;
            });
        });
    }

    private async Task SetAutostartAsync(bool enabled)
    {
        try
        {
            OnSettingsChanged(() => _settingsService.SetAutostartEnabled(enabled));

            if (enabled)
                await _autostartService.EnableAsync();
            else
                await _autostartService.DisableAsync();
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, "Settings", $"Autostart update failed: {ex.Message}", ex);
            await _notifier.NotifyErrorAsync($"Autostart update failed: {ex.Message}", ex, NotificationScenario.SettingsSaved);
        }
    }

    private void OnSettingsChanged(Action settingAction)
    {
        settingAction();
        _ = _settingsService.SaveAsync();
        _logger.Log(LogLevel.Info, "Settings", "Settings saved");
        _ = _notifier.NotifyAsync("Settings saved", NotificationScenario.SettingsSaved, NotificationType.Info);
    }

    private void VolumeSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        var vol = (int)e.NewValue;
        OnSettingsChanged(() => _settingsService.SetVolume(vol));
        VolumeLabel.Text = $"{vol}%";
    }

    private void ScalingModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ScalingModeComboBox.SelectedItem is string mode)
        {
            OnSettingsChanged(() => _settingsService.SetScalingMode(mode));
        }
    }

    private void ThemePreferenceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemePreferenceComboBox.SelectedItem is not string pref)
            return;

        _settingsService.SetThemePreference(pref);
        _ = _settingsService.SaveAsync();
        System.Diagnostics.Debug.WriteLine($"ThemePreference changed to {pref}");
        _logger.Log(LogLevel.Info, "Settings", $"Theme changed: {pref}");
        Dispatcher.Invoke(() => ThemeService.ApplyTheme(App.Current, ThemeService.ResolveIsLightTheme(_settingsService)));
        _ = _notifier.NotifyAsync("Settings saved", NotificationScenario.SettingsSaved, NotificationType.Info);
    }

    private void ApplyAutoplayDelay()
    {
        if (_autoplayDelayTextBox == null)
            return;

        if (int.TryParse(_autoplayDelayTextBox.Text, out var seconds))
        {
            seconds = Math.Max(0, seconds);
            _autoplayDelayTextBox.Text = seconds.ToString();
            OnSettingsChanged(() => _settingsService.SetAutoplayDelaySeconds(seconds));
        }
        else
        {
            _autoplayDelayTextBox.Text = _settingsService.GetAutoplayDelaySeconds().ToString();
        }
    }
}
