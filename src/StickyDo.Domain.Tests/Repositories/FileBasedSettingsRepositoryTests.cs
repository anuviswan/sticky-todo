using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Constants;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Storage;

namespace StickyDo.Domain.Tests.Repositories;

[TestClass]
public class FileBasedSettingsRepositoryTests
{
    private string _testRootDirectory = null!;
    private IStorageLocationProvider _storageLocationProvider = null!;
    private FileBasedSettingsRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        _testRootDirectory = Path.Combine(Path.GetTempPath(), "StickyDo_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRootDirectory);
        _storageLocationProvider = new FakeStorageLocationProvider(_testRootDirectory);
        _repository = new FileBasedSettingsRepository(_storageLocationProvider);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testRootDirectory))
            Directory.Delete(_testRootDirectory, recursive: true);
    }

    [TestMethod]
    public async Task LoadAsync_ReturnsDefaults_WhenNoSettingsFileExists()
    {
        var settings = await _repository.LoadAsync();

        Assert.IsFalse(settings.LaunchAtStartup);
        Assert.AreEqual(ColorPalette.GetDefaultColor(), settings.DefaultNoteColor);
    }

    [TestMethod]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsSettings()
    {
        var color = ColorPalette.Colors[3];
        var settings = new AppSettings { LaunchAtStartup = true, DefaultNoteColor = color };

        await _repository.SaveAsync(settings);
        var loaded = await _repository.LoadAsync();

        Assert.IsTrue(loaded.LaunchAtStartup);
        Assert.AreEqual(color, loaded.DefaultNoteColor);
    }

    [TestMethod]
    public async Task LoadAsync_ReturnsDefaultsAndQuarantinesFile_WhenSettingsFileIsCorrupt()
    {
        var settingsFilePath = Path.Combine(_storageLocationProvider.SettingsDirectory, "settings.json");
        Directory.CreateDirectory(_storageLocationProvider.SettingsDirectory);
        await File.WriteAllTextAsync(settingsFilePath, "{ not valid json");

        var settings = await _repository.LoadAsync();

        Assert.IsFalse(settings.LaunchAtStartup);
        Assert.AreEqual(ColorPalette.GetDefaultColor(), settings.DefaultNoteColor);
        Assert.IsFalse(File.Exists(settingsFilePath));
        Assert.IsTrue(File.Exists(settingsFilePath + ".corrupt"));
    }

    [TestMethod]
    public async Task LoadAsync_FallsBackToDefaultColor_WhenPersistedColorIsNotInPalette()
    {
        var settingsFilePath = Path.Combine(_storageLocationProvider.SettingsDirectory, "settings.json");
        Directory.CreateDirectory(_storageLocationProvider.SettingsDirectory);
        await File.WriteAllTextAsync(settingsFilePath, "{ \"LaunchAtStartup\": true, \"DefaultNoteColor\": 12345 }");

        var settings = await _repository.LoadAsync();

        Assert.IsTrue(settings.LaunchAtStartup);
        Assert.AreEqual(ColorPalette.GetDefaultColor(), settings.DefaultNoteColor);
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
