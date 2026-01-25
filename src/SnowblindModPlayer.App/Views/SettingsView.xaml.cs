using System.Windows.Controls;
using SnowblindModPlayer.UI.ViewModels;

namespace SnowblindModPlayer.Views;

public partial class SettingsView : UserControl
{
    public SettingsView(MonitorSelectionViewModel monitorSelectionViewModel)
    {
        InitializeComponent();
        
        // Embed the MonitorSelectionView
        var monitorView = new MonitorSelectionView(monitorSelectionViewModel);
        MonitorSelectionHost.Content = monitorView;
    }
}
