namespace StickyDo.Domain.Services;

/// <summary>
/// Exports the user's notes to a backup file, and imports them back from one.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Zips the current note data files, plus a manifest recording <paramref name="appVersion"/>,
    /// into a single archive at <paramref name="filePath"/>.
    /// </summary>
    /// <returns>The number of note files included in the archive.</returns>
    Task<int> ExportAsync(string filePath, string appVersion);

    /// <summary>
    /// Restores notes from a backup archive previously created by <see cref="ExportAsync"/>,
    /// writing each note's JSON file into the data directory. A note whose ID collides with one
    /// that already exists on disk is imported as a copy under a new ID, so existing notes are
    /// always preserved. Entries that fail to deserialize are skipped rather than aborting the
    /// whole import.
    /// </summary>
    /// <returns>The number of notes successfully imported.</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException"><paramref name="filePath"/> does not exist.</exception>
    /// <exception cref="InvalidDataException">
    /// The file is not a zip archive, or doesn't contain a StickyDo backup manifest.
    /// </exception>
    Task<int> ImportAsync(string filePath);
}
