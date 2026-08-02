using System.IO.Compression;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Serialization;
using StickyDo.Domain.Services;
using StickyDo.Domain.Storage;

namespace StickyDo.Domain.Tests.Services;

[TestClass]
public class BackupServiceTests
{
    private string _testDirectory = null!;
    private FakeStorageLocationProvider _storageLocationProvider = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "StickyDo_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _storageLocationProvider = new FakeStorageLocationProvider(_testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [TestMethod]
    public async Task ExportAsync_ZipsNoteFilesUnderNotesFolder_AndReturnsCount()
    {
        Directory.CreateDirectory(_storageLocationProvider.DataDirectory);
        File.WriteAllText(Path.Combine(_storageLocationProvider.DataDirectory, "note1.json"), """{"Title":"Note 1"}""");
        File.WriteAllText(Path.Combine(_storageLocationProvider.DataDirectory, "note2.json"), """{"Title":"Note 2"}""");
        var service = new BackupService(_storageLocationProvider);
        var zipPath = Path.Combine(_testDirectory, "backup.zip");

        var exportedCount = await service.ExportAsync(zipPath, "1.2.3");

        Assert.AreEqual(2, exportedCount);
        Assert.IsTrue(File.Exists(zipPath));

        using var archive = ZipFile.OpenRead(zipPath);
        CollectionAssert.AreEquivalent(
            new[] { "manifest.json", "Notes/note1.json", "Notes/note2.json" },
            archive.Entries.Select(e => e.FullName).ToArray());
    }

    [TestMethod]
    public async Task ExportAsync_WritesManifestWithAppVersionAndCount()
    {
        Directory.CreateDirectory(_storageLocationProvider.DataDirectory);
        File.WriteAllText(Path.Combine(_storageLocationProvider.DataDirectory, "note1.json"), """{"Title":"Note 1"}""");
        var service = new BackupService(_storageLocationProvider);
        var zipPath = Path.Combine(_testDirectory, "backup.zip");

        var before = DateTime.UtcNow;
        await service.ExportAsync(zipPath, "1.2.3");
        var after = DateTime.UtcNow;

        using var archive = ZipFile.OpenRead(zipPath);
        var manifestEntry = archive.GetEntry("manifest.json");
        Assert.IsNotNull(manifestEntry);

        using var reader = new StreamReader(manifestEntry!.Open());
        var manifest = JsonSerializer.Deserialize<BackupManifest>(reader.ReadToEnd(), JsonSerializationOptions.Default);

        Assert.IsNotNull(manifest);
        Assert.AreEqual("1.2.3", manifest!.AppVersion);
        Assert.AreEqual(1, manifest.NoteCount);
        Assert.IsTrue(manifest.ExportedAtUtc >= before && manifest.ExportedAtUtc <= after);
    }

    [TestMethod]
    public async Task ExportAsync_ExcludesTmpAndCorruptFiles()
    {
        Directory.CreateDirectory(_storageLocationProvider.DataDirectory);
        File.WriteAllText(Path.Combine(_storageLocationProvider.DataDirectory, "note1.json"), """{"Title":"Note 1"}""");
        File.WriteAllText(Path.Combine(_storageLocationProvider.DataDirectory, "note2.json.tmp"), "partial");
        File.WriteAllText(Path.Combine(_storageLocationProvider.DataDirectory, "note3.json.corrupt"), "garbage");
        var service = new BackupService(_storageLocationProvider);
        var zipPath = Path.Combine(_testDirectory, "backup.zip");

        var exportedCount = await service.ExportAsync(zipPath, "1.0.0");

        Assert.AreEqual(1, exportedCount);
        using var archive = ZipFile.OpenRead(zipPath);
        CollectionAssert.AreEquivalent(
            new[] { "manifest.json", "Notes/note1.json" },
            archive.Entries.Select(e => e.FullName).ToArray());
    }

    [TestMethod]
    public async Task ExportAsync_WithNoNotes_CreatesZipWithManifestOnly()
    {
        var service = new BackupService(_storageLocationProvider);
        var zipPath = Path.Combine(_testDirectory, "backup.zip");

        var exportedCount = await service.ExportAsync(zipPath, "1.0.0");

        Assert.AreEqual(0, exportedCount);
        using var archive = ZipFile.OpenRead(zipPath);
        CollectionAssert.AreEquivalent(new[] { "manifest.json" }, archive.Entries.Select(e => e.FullName).ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task ExportAsync_WithEmptyPath_Throws()
    {
        var service = new BackupService(_storageLocationProvider);

        await service.ExportAsync(string.Empty, "1.0.0");
    }

    [TestMethod]
    public async Task ImportAsync_ImportsNoteFilesFromNotesFolder_AndReturnsCount()
    {
        Directory.CreateDirectory(_storageLocationProvider.DataDirectory);
        var exportService = new BackupService(_storageLocationProvider);
        File.WriteAllText(
            Path.Combine(_storageLocationProvider.DataDirectory, $"{Guid.NewGuid():N}.json"),
            JsonSerializer.Serialize(new StickyNote { Id = Guid.NewGuid(), Title = "Note 1" }, JsonSerializationOptions.Default));
        File.WriteAllText(
            Path.Combine(_storageLocationProvider.DataDirectory, $"{Guid.NewGuid():N}.json"),
            JsonSerializer.Serialize(new StickyNote { Id = Guid.NewGuid(), Title = "Note 2" }, JsonSerializationOptions.Default));
        var zipPath = Path.Combine(_testDirectory, "backup.zip");
        await exportService.ExportAsync(zipPath, "1.0.0");

        // Simulate a fresh install with no existing notes.
        Directory.Delete(_storageLocationProvider.DataDirectory, recursive: true);
        var importService = new BackupService(_storageLocationProvider);

        var importedCount = await importService.ImportAsync(zipPath);

        Assert.AreEqual(2, importedCount);
        var importedTitles = Directory.GetFiles(_storageLocationProvider.DataDirectory, "*.json")
            .Select(f => JsonSerializer.Deserialize<StickyNote>(File.ReadAllText(f), JsonSerializationOptions.Default)!.Title)
            .ToList();
        CollectionAssert.AreEquivalent(new[] { "Note 1", "Note 2" }, importedTitles);
    }

    [TestMethod]
    public async Task ImportAsync_WhenNoteIdCollidesWithExisting_PreservesExistingAndAssignsNewIdToImported()
    {
        Directory.CreateDirectory(_storageLocationProvider.DataDirectory);
        var sharedId = Guid.NewGuid();
        var existingNotePath = Path.Combine(_storageLocationProvider.DataDirectory, $"{sharedId:N}.json");
        File.WriteAllText(
            existingNotePath,
            JsonSerializer.Serialize(new StickyNote { Id = sharedId, Title = "Original" }, JsonSerializationOptions.Default));

        var zipPath = Path.Combine(_testDirectory, "backup.zip");
        using (var zipStream = new FileStream(zipPath, FileMode.Create))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            using (var writer = new StreamWriter(archive.CreateEntry("manifest.json").Open()))
                writer.Write("{}");
            using var noteWriter = new StreamWriter(archive.CreateEntry($"Notes/{sharedId:N}.json").Open());
            noteWriter.Write(JsonSerializer.Serialize(new StickyNote { Id = sharedId, Title = "Imported" }, JsonSerializationOptions.Default));
        }

        var service = new BackupService(_storageLocationProvider);
        var importedCount = await service.ImportAsync(zipPath);

        Assert.AreEqual(1, importedCount);
        var originalStillExists = JsonSerializer.Deserialize<StickyNote>(
            File.ReadAllText(existingNotePath), JsonSerializationOptions.Default);
        Assert.AreEqual("Original", originalStillExists!.Title);

        var allTitles = Directory.GetFiles(_storageLocationProvider.DataDirectory, "*.json")
            .Select(f => JsonSerializer.Deserialize<StickyNote>(File.ReadAllText(f), JsonSerializationOptions.Default)!.Title)
            .ToList();
        CollectionAssert.AreEquivalent(new[] { "Original", "Imported" }, allTitles);
    }

    [TestMethod]
    public async Task ImportAsync_SkipsCorruptEntries_AndImportsTheRest()
    {
        var zipPath = Path.Combine(_testDirectory, "backup.zip");
        using (var zipStream = new FileStream(zipPath, FileMode.Create))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            using (var writer = new StreamWriter(archive.CreateEntry("manifest.json").Open()))
                writer.Write("{}");
            using (var writer = new StreamWriter(archive.CreateEntry("Notes/good.json").Open()))
                writer.Write(JsonSerializer.Serialize(new StickyNote { Id = Guid.NewGuid(), Title = "Good" }, JsonSerializationOptions.Default));
            using (var writer = new StreamWriter(archive.CreateEntry("Notes/bad.json").Open()))
                writer.Write("not valid json");
        }

        var service = new BackupService(_storageLocationProvider);
        var importedCount = await service.ImportAsync(zipPath);

        Assert.AreEqual(1, importedCount);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidDataException))]
    public async Task ImportAsync_WithMissingManifest_Throws()
    {
        var zipPath = Path.Combine(_testDirectory, "backup.zip");
        using (var zipStream = new FileStream(zipPath, FileMode.Create))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            using var writer = new StreamWriter(archive.CreateEntry("Notes/note1.json").Open());
            writer.Write(JsonSerializer.Serialize(new StickyNote { Id = Guid.NewGuid(), Title = "Note 1" }, JsonSerializationOptions.Default));
        }

        var service = new BackupService(_storageLocationProvider);
        await service.ImportAsync(zipPath);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidDataException))]
    public async Task ImportAsync_WithNonZipFile_Throws()
    {
        var filePath = Path.Combine(_testDirectory, "not-a-zip.zip");
        File.WriteAllText(filePath, "this is plain text, not a zip archive");
        var service = new BackupService(_storageLocationProvider);

        await service.ImportAsync(filePath);
    }

    [TestMethod]
    [ExpectedException(typeof(FileNotFoundException))]
    public async Task ImportAsync_WithMissingFile_Throws()
    {
        var service = new BackupService(_storageLocationProvider);

        await service.ImportAsync(Path.Combine(_testDirectory, "does-not-exist.zip"));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task ImportAsync_WithEmptyPath_Throws()
    {
        var service = new BackupService(_storageLocationProvider);

        await service.ImportAsync(string.Empty);
    }

    private sealed class FakeStorageLocationProvider : IStorageLocationProvider
    {
        public FakeStorageLocationProvider(string root)
        {
            RootDirectory = root;
            DataDirectory = Path.Combine(root, "Data");
            SettingsDirectory = Path.Combine(root, "Settings");
            LogsDirectory = Path.Combine(root, "Logs");
            BackupsDirectory = Path.Combine(root, "Backups");
        }

        public string RootDirectory { get; }
        public string DataDirectory { get; }
        public string SettingsDirectory { get; }
        public string LogsDirectory { get; }
        public string BackupsDirectory { get; }
    }
}
