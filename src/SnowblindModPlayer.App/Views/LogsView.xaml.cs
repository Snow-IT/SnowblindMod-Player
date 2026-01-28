using System.Windows.Controls;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace SnowblindModPlayer.Views;

public partial class LogsView : UserControl
{
    private LogsViewModel? _viewModel;

    public LogsView()
    {
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        // Wire LogsViewModel AFTER InitializeComponent (XAML binding-safe)
        try
        {
            var serviceProvider = System.Windows.Application.Current.Resources["ServiceProvider"] as IServiceProvider;
            if (serviceProvider != null)
            {
                _viewModel = serviceProvider.GetRequiredService<LogsViewModel>();
                DataContext = _viewModel;
                System.Diagnostics.Debug.WriteLine("? LogsViewModel wired to LogsView");
                
                // Refresh logs when view is initialized (RefreshCommand internally calls RefreshLogsAsync)
                _viewModel.RefreshCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Failed to wire LogsViewModel: {ex.Message}");
        }
    }

    private void UserControl_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        // Refresh logs when user navigates to Logs tab
        if ((bool)e.NewValue && _viewModel != null)
        {
            System.Diagnostics.Debug.WriteLine("?? Logs tab visible - refreshing logs");
            _viewModel.RefreshCommand.Execute(null);
        }
    }
}
