using System.Diagnostics;
using System.Runtime.Versioning;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public class TaskSchedulerAutostartService : IAutostartService
{
    private const string TaskName = "SnowblindModPlayer";
    private readonly ILoggingService _logger;

    public TaskSchedulerAutostartService(ILoggingService logger)
    {
        _logger = logger;
    }

    public bool IsEnabled()
    {
        try
        {
            var task = GetTask();
            return task?.Enabled == true;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, "Autostart", $"IsEnabled failed: {ex.Message}", ex);
            return false;
        }
    }

    public Task EnableAsync()
    {
        var exePath = GetExecutablePath();
        if (string.IsNullOrWhiteSpace(exePath))
            return Task.CompletedTask;

        var service = CreateService();
        dynamic root = service.GetFolder("\\");

        dynamic taskDefinition = service.NewTask(0);
        taskDefinition.RegistrationInfo.Description = "Starts Snowblind-Mod Player on user logon.";

        dynamic trigger = taskDefinition.Triggers.Create(TaskTriggerLogon);
        trigger.UserId = Environment.UserName;

        dynamic action = taskDefinition.Actions.Create(TaskActionExec);
        action.Path = exePath;
        action.Arguments = "--tray";

        taskDefinition.Settings.DisallowStartIfOnBatteries = false;
        taskDefinition.Settings.StopIfGoingOnBatteries = false;
        taskDefinition.Settings.StartWhenAvailable = true;
        taskDefinition.Settings.Hidden = true;

        root.RegisterTaskDefinition(TaskName, taskDefinition, TaskCreateOrUpdate, null, null, TaskLogonInteractiveToken);
        _logger.Log(LogLevel.Info, "Autostart", "Task Scheduler autostart enabled");

        return Task.CompletedTask;
    }

    public Task DisableAsync()
    {
        var service = CreateService();
        dynamic root = service.GetFolder("\\");
        var task = GetTask();
        if (task != null)
        {
            root.DeleteTask(TaskName, 0);
            _logger.Log(LogLevel.Info, "Autostart", "Task Scheduler autostart disabled");
        }

        return Task.CompletedTask;
    }

    private string? GetExecutablePath()
    {
        try
        {
            return Process.GetCurrentProcess().MainModule?.FileName;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, "Autostart", $"GetExecutablePath failed: {ex.Message}", ex);
            return null;
        }
    }

    private dynamic? GetTask()
    {
        var service = CreateService();
        dynamic root = service.GetFolder("\\");
        try
        {
            return root.GetTask(TaskName);
        }
        catch
        {
            return null;
        }
    }

    private static dynamic CreateService()
    {
        var type = Type.GetTypeFromProgID("Schedule.Service")
                   ?? throw new InvalidOperationException("Task Scheduler COM service not available.");
        dynamic service = Activator.CreateInstance(type)!;
        service.Connect();
        return service;
    }

    private const int TaskTriggerLogon = 9;
    private const int TaskActionExec = 0;
    private const int TaskCreateOrUpdate = 6;
    private const int TaskLogonInteractiveToken = 3;
}
