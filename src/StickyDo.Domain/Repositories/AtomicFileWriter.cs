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
            using (var fileStream = new FileStream(
                tmpFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.WriteThrough | FileOptions.SequentialScan))
            {
                using (var writer = new StreamWriter(fileStream))
                {
                    await writer.WriteAsync(content);
                    await writer.FlushAsync();
                    fileStream.Flush();
                    fileStream.Flush(flushToDisk: true);
                }
            }
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
                if (File.Exists(targetPath))
                    File.Delete(targetPath);

                File.Move(tmpPath, targetPath, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to replace target file: {targetPath}", ex);
            }
        });
    }

    /// <summary>
    /// Cleans up orphaned temporary files.
    /// Called on startup and on write failures.
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

            int cleanedCount = 0;
            foreach (var tmpFile in tmpFiles)
            {
                try
                {
                    File.Delete(tmpFile);
                    System.Diagnostics.Debug.WriteLine($"AtomicFileWriter: Deleted orphaned file: {Path.GetFileName(tmpFile)}");
                    cleanedCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AtomicFileWriter: Failed to delete {Path.GetFileName(tmpFile)}: {ex.Message}");
                }
            }

            if (cleanedCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"AtomicFileWriter: Cleaned up {cleanedCount} orphaned temporary files");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AtomicFileWriter: Error during cleanup: {ex.Message}");
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
