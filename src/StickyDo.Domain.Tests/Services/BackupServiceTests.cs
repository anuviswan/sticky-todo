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
