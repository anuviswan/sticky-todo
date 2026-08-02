using System.IO;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Constants;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Domain.Storage;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Tests.ViewModels;

[TestClass]
public class SettingsViewModelTests
{
    private FakeSettingsRepository _repository = null!;
    private FakeBackupService _backupService = null!;
    private FakeFilePickerService _filePickerService = null!;
    private FakeDialogService _dialogService = null!;
    private FakeStorageLocationProvider _storageLocationProvider = null!;
    private SettingsViewModel _viewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeSettingsRepository();
        _backupService = new FakeBackupService();
        _filePickerService = new FakeFilePickerService();
        _dialogService = new FakeDialogService();
        _storageLocationProvider = new FakeStorageLocationProvider(
            Path.Combine(Path.GetTempPath(), "StickyDo_Tests", Guid.NewGuid().ToString()));
        _viewModel = new SettingsViewModel(
            _repository,
            _backupService,
            _filePickerService,
            _dialogService,
            _storageLocationProvider);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_storageLocationProvider.RootDirectory))
            Directory.Delete(_storageLocationProvider.RootDirectory, recursive: true);
    }

    [TestMethod]
    public void Constructor_DefaultsSelectedColorToPaletteDefault()
    {
        Assert.AreEqual(ColorPalette.GetDefaultColor(), _viewModel.SelectedDefaultColor);
    }

    [TestMethod]
    public void Constructor_LaunchAtStartupDefaultsToFalse()
    {
        Assert.IsFalse(_viewModel.LaunchAtStartup);
    }

    [TestMethod]
    public void SelectDefaultColor_UpdatesSelectedDefaultColor()
    {
        var color = ColorPalette.Colors[3];

        _viewModel.SelectDefaultColor(color);

        Assert.AreEqual(color, _viewModel.SelectedDefaultColor);
    }

    [TestMethod]
    public void Close_RaisesCloseRequested()
    {
        var raised = false;
        _viewModel.CloseRequested += (s, e) => raised = true;

        _viewModel.Close();

        Assert.IsTrue(raised);
    }

    [TestMethod]
    public async Task InitializeAsync_PopulatesPropertiesFromRepository_WithoutSaving()
    {
        var color = ColorPalette.Colors[2];
        _repository.StoredSettings = new AppSettings { LaunchAtStartup = true, DefaultNoteColor = color };

        await _viewModel.InitializeAsync();

        Assert.IsTrue(_viewModel.LaunchAtStartup);
        Assert.AreEqual(color, _viewModel.SelectedDefaultColor);
        Assert.AreEqual(0, _repository.SaveCallCount);
    }

    [TestMethod]
    public void ChangingLaunchAtStartup_SavesSettingsAutomatically()
    {
        _viewModel.LaunchAtStartup = true;

        Assert.AreEqual(1, _repository.SaveCallCount);
        Assert.IsTrue(_repository.StoredSettings!.LaunchAtStartup);
    }

    [TestMethod]
    public void SelectDefaultColor_SavesSettingsAutomatically()
    {
        var color = ColorPalette.Colors[4];

        _viewModel.SelectDefaultColor(color);

        Assert.AreEqual(1, _repository.SaveCallCount);
        Assert.AreEqual(color, _repository.StoredSettings!.DefaultNoteColor);
    }

    [TestMethod]
    public async Task ExportNotesAsync_WhenUserCancelsPicker_DoesNotExport()
    {
        _filePickerService.PathToReturn = null;

        await _viewModel.ExportNotesAsync();

        Assert.AreEqual(0, _backupService.ExportCallCount);
        Assert.AreEqual(0, _dialogService.MessageCallCount);
    }

    [TestMethod]
    public async Task ExportNotesAsync_CreatesBackupsDirectoryBeforeShowingDialog()
    {
        _filePickerService.PathToReturn = null;

        await _viewModel.ExportNotesAsync();

        Assert.IsTrue(Directory.Exists(_storageLocationProvider.BackupsDirectory));
    }

    [TestMethod]
    public async Task ExportNotesAsync_OnSuccess_ExportsToChosenPathAndShowsSuccessMessage()
    {
        var targetPath = Path.Combine(_storageLocationProvider.BackupsDirectory, "chosen.zip");
        _filePickerService.PathToReturn = targetPath;
        _backupService.CountToReturn = 3;

        await _viewModel.ExportNotesAsync();

        Assert.AreEqual(1, _backupService.ExportCallCount);
        Assert.AreEqual(targetPath, _backupService.LastFilePath);
        Assert.IsFalse(string.IsNullOrEmpty(_backupService.LastAppVersion));
        Assert.AreEqual(1, _dialogService.MessageCallCount);
        Assert.AreEqual(MessageBoxImage.Information, _dialogService.LastIcon);
    }

    [TestMethod]
    public async Task ExportNotesAsync_WhenBackupServiceThrows_ShowsErrorMessage()
    {
        _filePickerService.PathToReturn = Path.Combine(_storageLocationProvider.BackupsDirectory, "chosen.zip");
        _backupService.ExceptionToThrow = new IOException("disk full");

        await _viewModel.ExportNotesAsync();

        Assert.AreEqual(1, _dialogService.MessageCallCount);
        Assert.AreEqual(MessageBoxImage.Error, _dialogService.LastIcon);
    }

    private sealed class FakeSettingsRepository : ISettingsRepository
    {
        public AppSettings? StoredSettings { get; set; }
        public int SaveCallCount { get; private set; }

        public Task<AppSettings> LoadAsync() => Task.FromResult(StoredSettings ?? new AppSettings());

        public Task SaveAsync(AppSettings settings)
        {
            StoredSettings = settings;
            SaveCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBackupService : IBackupService
    {
        public int ExportCallCount { get; private set; }
        public string? LastFilePath { get; private set; }
        public string? LastAppVersion { get; private set; }
        public int CountToReturn { get; set; } = 1;
        public Exception? ExceptionToThrow { get; set; }

        public Task<int> ExportAsync(string filePath, string appVersion)
        {
            ExportCallCount++;
            LastFilePath = filePath;
            LastAppVersion = appVersion;

            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            return Task.FromResult(CountToReturn);
        }
    }

    private sealed class FakeFilePickerService : IFilePickerService
    {
        public string? PathToReturn { get; set; }

        public string? ShowSaveFileDialog(string defaultFileName, string filter, string? initialDirectory = null) =>
            PathToReturn;
    }

    private sealed class FakeDialogService : IDialogService
    {
        public int MessageCallCount { get; private set; }
        public MessageBoxImage LastIcon { get; private set; }

        public Task ShowMessageAsync(string title, string message, MessageBoxImage icon = MessageBoxImage.None)
        {
            MessageCallCount++;
            LastIcon = icon;
            return Task.CompletedTask;
        }

        public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(true);
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
