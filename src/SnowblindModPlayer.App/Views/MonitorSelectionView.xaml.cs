using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.UI.ViewModels;

namespace SnowblindModPlayer.Views;

public partial class MonitorSelectionView : UserControl
{
    private readonly MonitorSelectionViewModel _viewModel;

    public MonitorSelectionView(MonitorSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += (s, e) => DrawMonitors();
    }

    private void DrawMonitors()
    {
        MonitorCanvas.Children.Clear();

        var monitors = _viewModel.AvailableMonitors;
        if (monitors.Count == 0)
            return;

        // Calculate bounds
        int minX = monitors.Min(m => m.X);
        int minY = monitors.Min(m => m.Y);
        int maxX = monitors.Max(m => m.X + m.Width);
        int maxY = monitors.Max(m => m.Y + m.Height);

        int totalWidth = maxX - minX;
        int totalHeight = maxY - minY;

        // Scale factor to fit in canvas
        double canvasWidth = MonitorCanvas.ActualWidth - 20;
        double canvasHeight = MonitorCanvas.ActualHeight - 20;
        double scaleX = canvasWidth / totalWidth;
        double scaleY = canvasHeight / totalHeight;
        double scale = Math.Min(Math.Min(scaleX, scaleY), 0.5); // Cap at 0.5 to avoid huge rectangles

        // Draw each monitor
        foreach (var monitor in monitors)
        {
            double x = (monitor.X - minX) * scale + 10;
            double y = (monitor.Y - minY) * scale + 10;
            double width = monitor.Width * scale;
            double height = monitor.Height * scale;

            var rect = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = _viewModel.SelectedMonitor?.Id == monitor.Id ? new SolidColorBrush(Color.FromRgb(0, 120, 212)) : new SolidColorBrush(Colors.LightGray),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 2,
                Cursor = Cursors.Hand
            };

            rect.MouseDown += (s, e) =>
            {
                _viewModel.SelectedMonitor = monitor;
                DrawMonitors();
            };

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            MonitorCanvas.Children.Add(rect);

            // Add monitor label
            var label = new TextBlock
            {
                Text = monitor.DisplayName,
                Foreground = _viewModel.SelectedMonitor?.Id == monitor.Id ? Brushes.White : Brushes.Black,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var labelBg = new Border
            {
                Child = label,
                Width = width - 4,
                Height = height - 4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Canvas.SetLeft(labelBg, x + 2);
            Canvas.SetTop(labelBg, y + 2);
            MonitorCanvas.Children.Add(labelBg);
        }
    }
}
