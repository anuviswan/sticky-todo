using System.IO.Compression;
using System.Text.Json;
using StickyDo.Domain.Models;
using StickyDo.Domain.Serialization;
using StickyDo.Domain.Storage;
using StickyDo.Domain.Utilities;

namespace StickyDo.Domain.Services;

/// <summary>
/// File-based implementation of <see cref="IBackupService"/>. Zips the note JSON files
/// currently on disk (via <see cref="PersistencePathHelper.GetAllNoteFiles"/>, which already
/// excludes orphaned <c>.tmp</c>/<c>.corrupt</c> artifacts) into a <see cref="NotesFolderName"/>
/// folder alongside a <see cref="BackupManifest"/> - a direct copy of the real on-disk format
/// rather than a separately-maintained export schema.
/// </summary>
public class BackupService : IBackupService
{
    private const string NotesFolderName = "Notes";
    private const string ManifestFileName = "manifest.json";

    private readonly PersistencePathHelper _pathHelper;

    public BackupService(IStorageLocationProvider storageLocationProvider)
    {
        ArgumentNullException.ThrowIfNull(storageLocationProvider);
        _pathHelper = new PersistencePathHelper(storageLocationProvider);
    }

    public async Task<int> ExportAsync(string filePath, string appVersion)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path must be provided.", nameof(filePath));

        _pathHelper.EnsureDataDirectoryExists();
        var noteFiles = _pathHelper.GetAllNoteFiles().ToList();

        var manifest = new BackupManifest
        {
            AppVersion = appVersion,
            ExportedAtUtc = DateTime.UtcNow,
            NoteCount = noteFiles.Count
        };
        var manifestJson = JsonSerializer.Serialize(manifest, JsonSerializationOptions.Default);

        await Task.Run(() =>
        {
            using var zipStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

            var manifestEntry = archive.CreateEntry(ManifestFileName);
            using (var entryStream = manifestEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                writer.Write(manifestJson);
            }

            foreach (var noteFile in noteFiles)
            {
                archive.CreateEntryFromFile(noteFile, $"{NotesFolderName}/{Path.GetFileName(noteFile)}");
            }
        });

        return noteFiles.Count;
    }
}
