namespace StickyDo.Domain.Storage;

/// <summary>
/// Resolves the application's storage root and purpose-specific subdirectories.
/// The single source of truth for where StickyDo data lives on disk; storage
/// consumers must obtain paths through this abstraction rather than constructing
/// them directly.
/// </summary>
public interface IStorageLocationProvider
{
    /// <summary>
    /// The application's storage root directory (e.g. %LocalAppData%\DefineStack\StickyDO).
    /// </summary>
    string RootDirectory { get; }

    /// <summary>
    /// Directory for note/task data files.
    /// </summary>
    string DataDirectory { get; }

    /// <summary>
    /// Directory for application settings.
    /// </summary>
    string SettingsDirectory { get; }

    /// <summary>
    /// Directory for log files.
    /// </summary>
    string LogsDirectory { get; }

    /// <summary>
    /// Directory for backup files.
    /// </summary>
    string BackupsDirectory { get; }
}
