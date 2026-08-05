using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Utilities;
using Windows.ApplicationModel;

namespace StickyDo.Widget.Services;

/// <summary>
/// WinRT-backed implementation of <see cref="IStartupTaskService"/> using
/// <see cref="StartupTask"/>. Requires the app to be running with package identity (i.e.
/// launched from its MSIX install) and a matching <c>windows.startupTask</c> extension declared
/// in the package manifest; any failure (including running unpackaged) is caught and reported as
/// <see cref="StartupTaskStatus.Failed"/> rather than crashing the caller.
/// </summary>
public class StartupTaskService : IStartupTaskService
{
    private const string StartupTaskId = "StickyDoStartupTask";

    public async Task<StartupTaskStatus> GetStatusAsync()
    {
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            return Map(task.State);
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(GetStatusAsync));
            return StartupTaskStatus.Failed;
        }
    }

    public async Task<StartupTaskStatus> EnableAsync()
    {
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            var newState = await task.RequestEnableAsync();
            return Map(newState);
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(EnableAsync));
            return StartupTaskStatus.Failed;
        }
    }

    public async Task DisableAsync()
    {
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            task.Disable();
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(DisableAsync));
        }
    }

    private static StartupTaskStatus Map(StartupTaskState state) => state switch
    {
        StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy => StartupTaskStatus.Enabled,
        StartupTaskState.DisabledByUser => StartupTaskStatus.DisabledByUser,
        StartupTaskState.DisabledByPolicy => StartupTaskStatus.DisabledByPolicy,
        _ => StartupTaskStatus.Disabled
    };
}
