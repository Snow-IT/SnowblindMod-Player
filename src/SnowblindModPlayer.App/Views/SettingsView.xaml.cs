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
    private TextBox? _autoplayDelayTextBox;

    public SettingsView(MonitorSelectionViewModel monitorSelectionViewModel, ISettingsService settingsService)
    {
        _settingsService = settingsService;

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
        LoopEnabledCheckBox.Checked += (s, e) => { _settingsService.SetLoopEnabled(true); _ = _settingsService.SaveAsync(); };
        LoopEnabledCheckBox.Unchecked += (s, e) => { _settingsService.SetLoopEnabled(false); _ = _settingsService.SaveAsync(); };

        FullscreenOnStartCheckBox.IsChecked = _settingsService.GetFullscreenOnStart();
        FullscreenOnStartCheckBox.Checked += (s, e) => { _settingsService.SetFullscreenOnStart(true); _ = _settingsService.SaveAsync(); };
        FullscreenOnStartCheckBox.Unchecked += (s, e) => { _settingsService.SetFullscreenOnStart(false); _ = _settingsService.SaveAsync(); };

        MuteOnStartupCheckBox.IsChecked = _settingsService.GetMuted();
        MuteOnStartupCheckBox.Checked += (s, e) => { _settingsService.SetMuted(true); _ = _settingsService.SaveAsync(); };
        MuteOnStartupCheckBox.Unchecked += (s, e) => { _settingsService.SetMuted(false); _ = _settingsService.SaveAsync(); };

        VolumeSlider.Value = _settingsService.GetVolume();
        VolumeLabel.Text = $"{_settingsService.GetVolume()}%";
        VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;

        ScalingModeComboBox.ItemsSource = new[] { "Fill", "KeepAspect" };
        ScalingModeComboBox.SelectedItem = _settingsService.GetScalingMode();
        ScalingModeComboBox.SelectionChanged += ScalingModeComboBox_SelectionChanged;

        MinimizeToTrayOnStartupCheckBox.IsChecked = _settingsService.GetMinimizeToTrayOnStartup();
        MinimizeToTrayOnStartupCheckBox.Checked += (s, e) => { _settingsService.SetMinimizeToTrayOnStartup(true); _ = _settingsService.SaveAsync(); };
        MinimizeToTrayOnStartupCheckBox.Unchecked += (s, e) => { _settingsService.SetMinimizeToTrayOnStartup(false); _ = _settingsService.SaveAsync(); };

        var autoplayEnabledCheckBox = (CheckBox)FindName("AutoplayEnabledCheckBox");
        if (autoplayEnabledCheckBox != null)
        {
            autoplayEnabledCheckBox.IsChecked = _settingsService.GetAutoplayEnabled();
            autoplayEnabledCheckBox.Checked += (s, e) => { _settingsService.SetAutoplayEnabled(true); _ = _settingsService.SaveAsync(); };
            autoplayEnabledCheckBox.Unchecked += (s, e) => { _settingsService.SetAutoplayEnabled(false); _ = _settingsService.SaveAsync(); };
        }

        var autoplayDelayTextBox = (TextBox)FindName("AutoplayDelayTextBox");
        if (autoplayDelayTextBox != null)
        {
            _autoplayDelayTextBox = autoplayDelayTextBox;
            _autoplayDelayTextBox.Text = _settingsService.GetAutoplayDelaySeconds().ToString();
            _autoplayDelayTextBox.LostFocus += (s, e) => ApplyAutoplayDelay();
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

    private void VolumeSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        var vol = (int)e.NewValue;
        _settingsService.SetVolume(vol);
        VolumeLabel.Text = $"{vol}%";
        _ = _settingsService.SaveAsync();
    }

    private void ScalingModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ScalingModeComboBox.SelectedItem is string mode)
        {
            _settingsService.SetScalingMode(mode);
            _ = _settingsService.SaveAsync();
        }
    }

    private void ThemePreferenceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemePreferenceComboBox.SelectedItem is not string pref)
            return;

        _settingsService.SetThemePreference(pref);
        _ = _settingsService.SaveAsync();
        System.Diagnostics.Debug.WriteLine($"ThemePreference changed to {pref}");
        Dispatcher.Invoke(() => ThemeService.ApplyTheme(App.Current, ThemeService.ResolveIsLightTheme(_settingsService)));
    }

    private void ApplyAutoplayDelay()
    {
        if (_autoplayDelayTextBox == null)
            return;

        if (int.TryParse(_autoplayDelayTextBox.Text, out var seconds))
        {
            seconds = Math.Max(0, seconds);
            _autoplayDelayTextBox.Text = seconds.ToString();
            _settingsService.SetAutoplayDelaySeconds(seconds);
            _ = _settingsService.SaveAsync();
        }
        else
        {
            _autoplayDelayTextBox.Text = _settingsService.GetAutoplayDelaySeconds().ToString();
        }
    }
}
