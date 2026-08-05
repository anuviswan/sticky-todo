namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Outcome of querying or requesting a change to the app's Windows startup registration.
/// </summary>
public enum StartupTaskStatus
{
    /// <summary>The app is registered to launch at sign-in.</summary>
    Enabled,

    /// <summary>The app is not registered to launch at sign-in.</summary>
    Disabled,

    /// <summary>The user explicitly disabled the startup entry via Windows Settings.</summary>
    DisabledByUser,

    /// <summary>Group policy prevents the app from launching at sign-in.</summary>
    DisabledByPolicy,

    /// <summary>The registration state couldn't be determined or changed (e.g. no package identity, or a WinRT error).</summary>
    Failed
}

/// <summary>
/// Abstraction over Windows' <c>StartupTask</c> API for registering/unregistering the app to
/// launch at sign-in. Keeps ViewModels free of WinRT types and enables testing.
/// </summary>
public interface IStartupTaskService
{
    /// <summary>
    /// Queries the app's current startup registration state without changing it.
    /// </summary>
    Task<StartupTaskStatus> GetStatusAsync();

    /// <summary>
    /// Requests that the app be registered to launch at sign-in. May return a status other than
    /// <see cref="StartupTaskStatus.Enabled"/> if Windows denies or restricts the request.
    /// </summary>
    Task<StartupTaskStatus> EnableAsync();

    /// <summary>
    /// Removes the app's startup registration.
    /// </summary>
    Task DisableAsync();
}
