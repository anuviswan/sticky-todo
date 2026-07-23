using System.Text.Json;
using StickyDo.Domain.Models;
using StickyDo.Domain.Serialization;
using StickyDo.Domain.Utilities;

namespace StickyDo.Domain.Repositories;

/// <summary>
/// Implements atomic file write operations to prevent data corruption.
/// Pattern: Write to .tmp → Flush → Replace original → Delete temporary
/// </summary>
public static class AtomicFileWriter
{
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 100;

    /// <summary>
    /// Writes content to a file atomically to prevent data corruption.
    /// Retries on file lock errors (up to 3 times with exponential backoff).
    /// </summary>
    /// <param name="filePath">Path to the target file (e.g., guid.json)</param>
    /// <param name="content">JSON content to write</param>
    /// <exception cref="IOException">Thrown if write fails after retries</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if no write permission</exception>
    public static async Task WriteAtomicAsync(string filePath, string content)
    {
        var tmpFilePath = filePath + ".tmp";
        var retryCount = 0;

        while (retryCount < MaxRetries)
        {
            try
            {
                await WriteToTemporaryFileAsync(tmpFilePath, content);
                await ReplaceAtomicAsync(filePath, tmpFilePath);
                return;
            }
            catch (IOException) when (retryCount < MaxRetries - 1)
            {
                retryCount++;
                await Task.Delay(RetryDelayMs * retryCount);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (IOException ex)
            {
                CleanupTemporaryFile(tmpFilePath);
                throw new IOException(
                    $"Failed to write atomic file after {MaxRetries} retries: {filePath}", ex);
            }
        }

        CleanupTemporaryFile(tmpFilePath);
        throw new IOException($"Failed to write atomic file after {MaxRetries} retries: {filePath}");
    }

    /// <summary>
    /// Writes content to a temporary file with immediate disk flush.
    /// </summary>
    private static async Task WriteToTemporaryFileAsync(string tmpFilePath, string content)
    {
        try
        {
            using var fileStream = new FileStream(
                tmpFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.WriteThrough | FileOptions.SequentialScan);
            using var writer = new StreamWriter(fileStream);
            await writer.WriteAsync(content);
            await writer.FlushAsync();
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to write temporary file: {tmpFilePath}", ex);
        }
    }

    /// <summary>
    /// Atomically replaces the target file with the temporary file.
    /// Windows NTFS provides atomic rename at the filesystem level.
    /// </summary>
    private static async Task ReplaceAtomicAsync(string targetPath, string tmpPath)
    {
        await Task.Run(() =>
        {
            try
            {
                File.Move(tmpPath, targetPath, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to replace target file: {targetPath}", ex);
            }
        });
    }

    /// <summary>
    /// Cleans up orphaned temporary files intelligently:
    /// 1. If .json exists and is newer than or equal to .tmp, delete .tmp
    /// 2. If .json exists but .tmp is invalid JSON, delete .tmp
    /// 3. If no .json exists and .tmp is valid JSON, promote .tmp to .json
    /// 4. If no .json exists and .tmp is invalid JSON, delete .tmp
    /// </summary>
    public static void CleanupOrphanedTemporaryFiles()
    {
        try
        {
            var dataDir = PersistencePathHelper.GetDataDirectoryPath();
            if (!Directory.Exists(dataDir))
                return;

            var tmpFiles = Directory.GetFiles(dataDir, "*.json.tmp", SearchOption.TopDirectoryOnly);
            System.Diagnostics.Debug.WriteLine($"AtomicFileWriter: Found {tmpFiles.Length} orphaned .tmp files to clean up");

            var serializerOptions = JsonSerializationOptions.Default;
            int cleanedCount = 0;
            int promotedCount = 0;

            foreach (var tmpFile in tmpFiles)
            {
                try
                {
                    var originalFile = tmpFile[..^4]; // Remove .tmp extension
                    bool isValidJson = IsValidJsonFile(tmpFile, serializerOptions);

                    if (File.Exists(originalFile))
                    {
                        // Original file exists: delete .tmp only if original is newer/equal or .tmp is invalid
                        var originalUpdatedAt = GetStickyNoteUpdatedAt(originalFile, serializerOptions);
                        var tmpUpdatedAt = GetStickyNoteUpdatedAt(tmpFile, serializerOptions);

                        if (!isValidJson || (originalUpdatedAt.HasValue && tmpUpdatedAt.HasValue && originalUpdatedAt >= tmpUpdatedAt))
                        {
                            File.Delete(tmpFile);
                            System.Diagnostics.Debug.WriteLine($"AtomicFileWriter: Deleted outdated/invalid .tmp file: {Path.GetFileName(tmpFile)}");
                            cleanedCount++;
                        }
                    }
                    else if (isValidJson)
                    {
                        // No original file but .tmp is valid JSON: promote it
                        File.Move(tmpFile, originalFile, overwrite: false);
                        System.Diagnostics.Debug.WriteLine($"AtomicFileWriter: Promoted .tmp file to original: {Path.GetFileName(originalFile)}");
                        promotedCount++;
                    }
                    else
                    {
                        // No original file and .tmp is invalid: delete it
                        File.Delete(tmpFile);
                        System.Diagnostics.Debug.WriteLine($"AtomicFileWriter: Deleted invalid .tmp file: {Path.GetFileName(tmpFile)}");
                        cleanedCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AtomicFileWriter: Failed to process {Path.GetFileName(tmpFile)}: {ex.Message}");
                }
            }

            if (cleanedCount > 0 || promotedCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"AtomicFileWriter: Cleanup complete - deleted: {cleanedCount}, promoted: {promotedCount}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AtomicFileWriter: Error during cleanup: {ex.Message}");
        }
    }

    private static bool IsValidJsonFile(string filePath, JsonSerializerOptions options)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var content = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(content))
                return false;

            JsonSerializer.Deserialize<StickyNote>(content, options);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static DateTime? GetStickyNoteUpdatedAt(string filePath, JsonSerializerOptions options)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            var content = File.ReadAllText(filePath);
            var note = JsonSerializer.Deserialize<StickyNote>(content, options);
            return note?.UpdatedAt;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Cleans up a single temporary file.
    /// Silently ignores errors if file doesn't exist or is locked.
    /// </summary>
    private static void CleanupTemporaryFile(string tmpFilePath)
    {
        try
        {
            if (File.Exists(tmpFilePath))
                File.Delete(tmpFilePath);
        }
        catch
        {
            // Silently ignore cleanup errors
        }
    }
}
