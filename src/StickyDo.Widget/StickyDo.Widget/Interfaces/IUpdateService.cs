namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Outcome of an application update check, returned by <see cref="IUpdateService"/>.
/// </summary>
public class UpdateCheckResult
{
    /// <summary>The outcome of the check.</summary>
    public required UpdateCheckStatus Status { get; init; }

    /// <summary>The latest available version, set when <see cref="Status"/> is <see cref="UpdateCheckStatus.UpdateAvailable"/>.</summary>
    public string? LatestVersion { get; init; }

    /// <summary>A user-facing description of the failure, set when <see cref="Status"/> is <see cref="UpdateCheckStatus.Failed"/>.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// The result of checking whether a newer version of the app is available.
/// </summary>
public enum UpdateCheckStatus
{
    /// <summary>The running version is the latest available version.</summary>
    UpToDate,

    /// <summary>A newer version is available.</summary>
    UpdateAvailable,

    /// <summary>The check could not be completed.</summary>
    Failed
}

/// <summary>
/// Abstraction over checking whether a newer version of the app is available. Checks the
/// Microsoft Store update API first (the authoritative source once the app is Store-published),
/// falling back to the project's GitHub Releases when the Store check is unavailable - e.g.
/// during the sideloaded-MSIX phase before the Store listing exists. Keeps ViewModels free of
/// WinRT/HTTP types and enables testing.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Checks whether a newer version of the app is available.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdatesAsync();
}
