using System.IO.Compression;
using System.Text.Json;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
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

    public async Task<int> ImportAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path must be provided.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Backup file not found.", filePath);

        _pathHelper.EnsureDataDirectoryExists();

        using var zipStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        if (archive.GetEntry(ManifestFileName) is null)
            throw new InvalidDataException("The selected file is not a valid StickyDo backup.");

        var existingIds = _pathHelper.GetAllNoteFiles()
            .Select(PersistencePathHelper.ExtractNoteIdFromFilePath)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var importedCount = 0;
        var notesPrefix = $"{NotesFolderName}/";

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith(notesPrefix, StringComparison.Ordinal) ||
                !entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            StickyNote? note;
            try
            {
                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream);
                var json = await reader.ReadToEndAsync();
                note = JsonSerializer.Deserialize<StickyNote>(json, JsonSerializationOptions.Default);
            }
            catch (JsonException)
            {
                continue;
            }

            if (note is null)
                continue;

            if (existingIds.Contains(note.Id))
                note.Id = Guid.NewGuid();

            note.IsOpened = false;
            existingIds.Add(note.Id);

            var noteJson = JsonSerializer.Serialize(note, JsonSerializationOptions.Default);
            await AtomicFileWriter.WriteAtomicAsync(_pathHelper.GetNoteFilePath(note.Id), noteJson);
            importedCount++;
        }

        return importedCount;
    }
}
