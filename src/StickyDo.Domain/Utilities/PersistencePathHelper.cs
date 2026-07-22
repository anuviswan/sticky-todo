namespace StickyDo.Domain.Utilities;

/// <summary>
/// Utility for managing persistence paths and directories for the application.
/// Ensures consistent location for storing note data across platforms.
/// </summary>
public static class PersistencePathHelper
{
    private const string AppDataFolderName = "StickyTODO";

    /// <summary>
    /// Gets the application data directory path: %LocalAppData%\StickyTODO
    /// </summary>
    public static string GetDataDirectoryPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, AppDataFolderName);
    }

    /// <summary>
    /// Ensures the data directory exists, creating it if necessary.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown if no permission to create directory</exception>
    /// <exception cref="IOException">Thrown if directory creation fails for other I/O reasons</exception>
    public static void EnsureDataDirectoryExists()
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
    /// <returns>Path like: %LocalAppData%\StickyTODO\{guid}.json</returns>
    public static string GetNoteFilePath(Guid noteId)
    {
        var dataDir = GetDataDirectoryPath();
        return Path.Combine(dataDir, $"{noteId:N}.json");
    }

    /// <summary>
    /// Gets the temporary file path used during atomic writes.
    /// </summary>
    /// <param name="noteId">The note's GUID identifier</param>
    /// <returns>Path like: %LocalAppData%\StickyTODO\{guid}.json.tmp</returns>
    public static string GetNoteTemporaryFilePath(Guid noteId)
    {
        var dataDir = GetDataDirectoryPath();
        return Path.Combine(dataDir, $"{noteId:N}.json.tmp");
    }

    /// <summary>
    /// Gets the corrupt file path used when a JSON file cannot be deserialized.
    /// </summary>
    /// <param name="noteId">The note's GUID identifier</param>
    /// <returns>Path like: %LocalAppData%\StickyTODO\{guid}.json.corrupt</returns>
    public static string GetNoteCorruptFilePath(Guid noteId)
    {
        var dataDir = GetDataDirectoryPath();
        return Path.Combine(dataDir, $"{noteId:N}.json.corrupt");
    }

    /// <summary>
    /// Gets all note JSON files in the data directory.
    /// Excludes temporary and corrupt files.
    /// </summary>
    public static IEnumerable<string> GetAllNoteFiles()
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
