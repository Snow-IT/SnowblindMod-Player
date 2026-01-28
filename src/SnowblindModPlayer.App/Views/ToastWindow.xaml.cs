using System;
using System.Windows;
using System.Windows.Threading;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Views;

public partial class ToastWindow : Window
{
    private DispatcherTimer? _dismissTimer;
    private bool _positioningDone = false;
    private NotificationType _notificationType;

    /// <summary>
    /// Create and show a notification toast with title and message.
    /// </summary>
    public ToastWindow(string title, string message, NotificationType type = NotificationType.Info, int durationMs = 6000)
    {
        _notificationType = type;
        
        InitializeComponent();
        
        TitleBlock.Text = title;
        MessageBlock.Text = message;
        
        // Don't activate/focus the toast (background notification)
        ShowActivated = false;
        
        // Setup auto-dismiss timer
        _dismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(durationMs)
        };
        _dismissTimer.Tick += (s, e) =>
        {
            _dismissTimer.Stop();
            Close();
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Set type icon (window is now loaded)
        SetTypeIcon(_notificationType);
        
        // Position AFTER layout is calculated
        if (!_positioningDone)
        {
            PositionBottomRight();
            _positioningDone = true;
        }
        
        _dismissTimer?.Start();
        System.Diagnostics.Debug.WriteLine($"?? Toast loaded: {ActualWidth}x{ActualHeight} at ({Left},{Top})");
    }

    private void SetTypeIcon(NotificationType type)
    {
        // Emoji icons for different notification types
        TypeIcon.Text = type switch
        {
            NotificationType.Error => "?",
            NotificationType.Warning => "??",
            NotificationType.Success => "?",
            _ => "??"
        };
    }

    private void PositionBottomRight()
    {
        // Get working area of primary monitor
        var workArea = SystemParameters.WorkArea;
        
        // Position: bottom-right with margins
        Left = workArea.Right - ActualWidth - 20;
        Top = workArea.Bottom - ActualHeight - 20;

        System.Diagnostics.Debug.WriteLine($"   Toast positioned: ({Left},{Top}) [WorkArea: {workArea.Width}x{workArea.Height}]");
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        
        // Ensure window stays on top
        Topmost = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _dismissTimer?.Stop();
        _dismissTimer = null;
        base.OnClosed(e);
    }
}
