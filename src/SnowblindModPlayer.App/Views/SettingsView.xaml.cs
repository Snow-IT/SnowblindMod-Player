using System.Windows.Controls;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Services;
using SnowblindModPlayer.Services;
using SnowblindModPlayer.UI.ViewModels;

namespace SnowblindModPlayer.Views;

public partial class SettingsView : UserControl
{
    private readonly ISettingsService _settingsService;

    public SettingsView(MonitorSelectionViewModel monitorSelectionViewModel, ISettingsService settingsService)
    {
        _settingsService = settingsService;

        InitializeComponent();
        
        // Embed the MonitorSelectionView
        var monitorView = new MonitorSelectionView(monitorSelectionViewModel);
        MonitorSelectionHost.Content = monitorView;

        ThemePreferenceComboBox.ItemsSource = new[] { "System", "Light", "Dark" };
        ThemePreferenceComboBox.SelectedItem = _settingsService.GetThemePreference();
        ThemePreferenceComboBox.SelectionChanged += ThemePreferenceComboBox_SelectionChanged;

        _settingsService.RegisterLiveUpdate<string>("ThemePreference", pref =>
        {
            Dispatcher.Invoke(() =>
            {
                if (ThemePreferenceComboBox.SelectedItem as string != pref)
                    ThemePreferenceComboBox.SelectedItem = pref;
            });
        });
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
}
