using System.IO.Compression;
using StickyDo.Domain.Storage;
using StickyDo.Domain.Utilities;

namespace StickyDo.Domain.Services;

/// <summary>
/// File-based implementation of <see cref="IBackupService"/>. Zips the note JSON files
/// currently on disk (via <see cref="PersistencePathHelper.GetAllNoteFiles"/>, which already
/// excludes orphaned <c>.tmp</c>/<c>.corrupt</c> artifacts) into a single archive - a direct
/// copy of the real on-disk format rather than a separately-maintained export schema.
/// </summary>
public class BackupService : IBackupService
{
    private readonly PersistencePathHelper _pathHelper;

    public BackupService(IStorageLocationProvider storageLocationProvider)
    {
        ArgumentNullException.ThrowIfNull(storageLocationProvider);
        _pathHelper = new PersistencePathHelper(storageLocationProvider);
    }

    public async Task<int> ExportAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path must be provided.", nameof(filePath));

        _pathHelper.EnsureDataDirectoryExists();
        var noteFiles = _pathHelper.GetAllNoteFiles().ToList();

        await Task.Run(() =>
        {
            using var zipStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

            foreach (var noteFile in noteFiles)
            {
                archive.CreateEntryFromFile(noteFile, Path.GetFileName(noteFile));
            }
        });

        return noteFiles.Count;
    }
}
