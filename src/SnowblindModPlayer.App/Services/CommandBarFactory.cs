using SnowblindModPlayer.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SnowblindModPlayer.Services;

public static class CommandBarFactory
{
    public static object? CreateForPage(string page, object? view)
    {
        return page switch
        {
            "Videos" => CreateVideos(view as SnowblindModPlayer.Views.VideosView),
            "Logs" => CreateLogs(view as SnowblindModPlayer.Views.LogsView),
            _ => null,
        };
    }

    private static UIElement? CreateVideos(SnowblindModPlayer.Views.VideosView? view)
    {
        if (view?.DataContext is not VideosViewModel vm)
            return null;

        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        var iconFont = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

        var addBtn = new Button
        {
            Content = new TextBlock { Text = "\uE710", FontFamily = iconFont, FontSize = 16 },
            ToolTip = Application.Current.Resources["Tooltip.AddVideo"] as string ?? "Add video",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Margin = new Thickness(0, 0, 8, 0),
            Command = vm.ImportCommand
        };

        var delBtn = new Button
        {
            Content = new TextBlock { Text = "\uE74D", FontFamily = iconFont, FontSize = 16 },
            ToolTip = Application.Current.Resources["Tooltip.RemoveVideo"] as string ?? "Remove selected video",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Margin = new Thickness(0, 0, 8, 0),
            Command = vm.RemoveCommand
        };

        var starBtn = new Button
        {
            Content = new TextBlock { Text = "\uE734", FontFamily = iconFont, FontSize = 16 },
            ToolTip = Application.Current.Resources["Tooltip.SetDefaultVideo"] as string ?? "Set as default video",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Command = vm.SetDefaultCommand
        };

        panel.Children.Add(addBtn);
        panel.Children.Add(delBtn);
        panel.Children.Add(starBtn);

        return panel;
    }

    private static UIElement? CreateLogs(SnowblindModPlayer.Views.LogsView? view)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var iconFont = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

        var refreshBtn = new Button
        {
            Content = new TextBlock { Text = "\uE72C", FontFamily = iconFont, FontSize = 16 },
            ToolTip = Application.Current.Resources["Tooltip.RefreshLogs"] as string ?? "Refresh logs",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Margin = new Thickness(0, 0, 8, 0)
        };

        var clearBtn = new Button
        {
            Content = new TextBlock { Text = "\uE74D", FontFamily = iconFont, FontSize = 16 },
            ToolTip = Application.Current.Resources["Tooltip.ClearLogs"] as string ?? "Clear logs",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Margin = new Thickness(0, 0, 8, 0)
        };

        var folderBtn = new Button
        {
            Content = new TextBlock { Text = "\uED25", FontFamily = iconFont, FontSize = 16 },
            ToolTip = Application.Current.Resources["Tooltip.OpenLogsFolder"] as string ?? "Open logs folder",
            Style = (Style)Application.Current.FindResource("IconButton")
        };


        // Try to wire commands if ViewModel is available
        if (view?.DataContext is SnowblindModPlayer.ViewModels.LogsViewModel vm)
        {
            refreshBtn.Command = vm.RefreshCommand;
            clearBtn.Command = vm.ClearLogsCommand;
            folderBtn.Command = vm.OpenLogsFolder;
        }

        panel.Children.Add(refreshBtn);
        panel.Children.Add(clearBtn);
        panel.Children.Add(folderBtn);

        return panel;
    }
}
