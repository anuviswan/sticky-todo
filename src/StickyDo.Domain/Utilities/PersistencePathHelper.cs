using StickyDo.Domain.Storage;

namespace StickyDo.Domain.Utilities;

/// <summary>
/// Centralizes all persistence path logic for consistent file management.
/// Justification: Encapsulates path construction, enables future platform-specific paths,
/// and provides single source of truth for file naming conventions (GUIDs, extensions, etc).
/// Obtains its storage root exclusively through <see cref="IStorageLocationProvider"/>.
/// </summary>
public class PersistencePathHelper(IStorageLocationProvider storageLocationProvider)
{
    private readonly IStorageLocationProvider _storageLocationProvider =
        storageLocationProvider ?? throw new ArgumentNullException(nameof(storageLocationProvider));

    /// <summary>
    /// Gets the application data directory path.
    /// </summary>
    public string GetDataDirectoryPath()
    {
        return _storageLocationProvider.DataDirectory;
    }

    /// <summary>
    /// Ensures the data directory exists, creating it if necessary.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown if no permission to create directory</exception>
    /// <exception cref="IOException">Thrown if directory creation fails for other I/O reasons</exception>
    public void EnsureDataDirectoryExists()
    {
        var dataDir = GetDataDirectoryPath();
        if (!Directory.Exists(dataDir))
        {
            try
            {
                Directory.CreateDirectory(dataDir);
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException(
                    $"No permission to create directory: {dataDir}. Please ensure you have write access to %LocalAppData%.");
            }
            catch (IOException ex)
            {
                throw new IOException(
                    $"Failed to create data directory: {dataDir}. Error: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets the file path for a note's JSON file.
    /// </summary>
    /// <param name="noteId">The note's GUID identifier</param>
    /// <returns>Path like: {DataDirectory}\{guid}.json</returns>
    public string GetNoteFilePath(Guid noteId)
    {
        var dataDir = GetDataDirectoryPath();
        return Path.Combine(dataDir, $"{noteId:N}.json");
    }

    /// <summary>
    /// Gets the temporary file path used during atomic writes.
    /// </summary>
    /// <param name="noteId">The note's GUID identifier</param>
    /// <returns>Path like: {DataDirectory}\{guid}.json.tmp</returns>
    public string GetNoteTemporaryFilePath(Guid noteId)
    {
        var dataDir = GetDataDirectoryPath();
        return Path.Combine(dataDir, $"{noteId:N}.json.tmp");
    }

    /// <summary>
    /// Gets the corrupt file path used when a JSON file cannot be deserialized.
    /// </summary>
    /// <param name="noteId">The note's GUID identifier</param>
    /// <returns>Path like: {DataDirectory}\{guid}.json.corrupt</returns>
    public string GetNoteCorruptFilePath(Guid noteId)
    {
        var dataDir = GetDataDirectoryPath();
        return Path.Combine(dataDir, $"{noteId:N}.json.corrupt");
    }

    /// <summary>
    /// Gets all note JSON files in the data directory.
    /// Excludes temporary and corrupt files.
    /// </summary>
    public IEnumerable<string> GetAllNoteFiles()
    {
        var dataDir = GetDataDirectoryPath();
        if (!Directory.Exists(dataDir))
            return [];

        return Directory.GetFiles(dataDir, "*.json", SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) &&
                       !f.EndsWith(".corrupt", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extracts the GUID from a note file path.
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <returns>The GUID if parsing succeeds; otherwise null</returns>
    public static Guid? ExtractNoteIdFromFilePath(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (Guid.TryParseExact(fileName, "N", out var id))
            return id;

        return null;
    }
}
