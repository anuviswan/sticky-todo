namespace StickyDo.Domain.Services;

/// <summary>
/// Exports the user's notes to a backup file for safekeeping.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Zips the current note data files, plus a manifest recording <paramref name="appVersion"/>,
    /// into a single archive at <paramref name="filePath"/>.
    /// </summary>
    /// <returns>The number of note files included in the archive.</returns>
    Task<int> ExportAsync(string filePath, string appVersion);
}
