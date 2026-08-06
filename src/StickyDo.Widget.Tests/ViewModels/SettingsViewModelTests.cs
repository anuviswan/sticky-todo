using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Constants;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Domain.Storage;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Messages;
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
    private FileBasedRepository _noteRepository = null!;
    private WeakReferenceMessenger _messenger = null!;
    private FakeUrlLauncherService _urlLauncherService = null!;
    private FakeStartupTaskService _startupTaskService = null!;
    private FakeUpdateService _updateService = null!;
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
        _noteRepository = new FileBasedRepository(_storageLocationProvider);
        _messenger = new WeakReferenceMessenger();
        _urlLauncherService = new FakeUrlLauncherService();
        _startupTaskService = new FakeStartupTaskService();
        _updateService = new FakeUpdateService();
        _viewModel = new SettingsViewModel(
            _repository,
            _backupService,
            _filePickerService,
            _dialogService,
            _storageLocationProvider,
            _noteRepository,
            _messenger,
            _urlLauncherService,
            _startupTaskService,
            _updateService);
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
    public async Task InitializeAsync_ReflectsActualStartupState_OverridingPersistedValue()
    {
        var color = ColorPalette.Colors[2];
        _repository.StoredSettings = new AppSettings { LaunchAtStartup = false, DefaultNoteColor = color };
        _startupTaskService.StatusToReturn = StartupTaskStatus.Enabled;

        await _viewModel.InitializeAsync();

        Assert.IsTrue(_viewModel.LaunchAtStartup);
        Assert.AreEqual(color, _viewModel.SelectedDefaultColor);
        Assert.AreEqual(0, _repository.SaveCallCount);
    }

    [TestMethod]
    public async Task InitializeAsync_WhenStartupServiceQueryFails_FallsBackToPersistedValue()
    {
        _repository.StoredSettings = new AppSettings { LaunchAtStartup = true };
        _startupTaskService.StatusToReturn = StartupTaskStatus.Failed;

        await _viewModel.InitializeAsync();

        Assert.IsTrue(_viewModel.LaunchAtStartup);
        Assert.AreEqual(0, _repository.SaveCallCount);
    }

    [TestMethod]
    public async Task RefreshStartupStateAsync_UpdatesToggle_WithoutSaving()
    {
        _startupTaskService.StatusToReturn = StartupTaskStatus.Enabled;

        await _viewModel.RefreshStartupStateAsync();

        Assert.IsTrue(_viewModel.LaunchAtStartup);
        Assert.AreEqual(0, _repository.SaveCallCount);
    }

    [TestMethod]
    public void ChangingLaunchAtStartupToTrue_RegistersStartupTaskAndSaves()
    {
        _viewModel.LaunchAtStartup = true;

        Assert.AreEqual(1, _startupTaskService.EnableCallCount);
        Assert.IsTrue(_viewModel.LaunchAtStartup);
        Assert.AreEqual(1, _repository.SaveCallCount);
        Assert.IsTrue(_repository.StoredSettings!.LaunchAtStartup);
    }

    [TestMethod]
    public void ChangingLaunchAtStartupToFalse_UnregistersStartupTaskAndSaves()
    {
        _viewModel.LaunchAtStartup = true;

        _viewModel.LaunchAtStartup = false;

        Assert.AreEqual(1, _startupTaskService.DisableCallCount);
        Assert.AreEqual(2, _repository.SaveCallCount);
        Assert.IsFalse(_repository.StoredSettings!.LaunchAtStartup);
    }

    [TestMethod]
    public void ChangingLaunchAtStartupToTrue_WhenPolicyDisables_RevertsToggleAndShowsWarning()
    {
        _startupTaskService.EnableResult = StartupTaskStatus.DisabledByPolicy;

        _viewModel.LaunchAtStartup = true;

        Assert.IsFalse(_viewModel.LaunchAtStartup);
        Assert.AreEqual(1, _dialogService.MessageCallCount);
        Assert.AreEqual(MessageBoxImage.Warning, _dialogService.LastIcon);
        Assert.AreEqual(0, _repository.SaveCallCount);
    }

    [TestMethod]
    public void ChangingLaunchAtStartupToTrue_WhenServiceThrows_RevertsToggleAndShowsWarning()
    {
        _startupTaskService.EnableException = new InvalidOperationException("boom");

        _viewModel.LaunchAtStartup = true;

        Assert.IsFalse(_viewModel.LaunchAtStartup);
        Assert.AreEqual(1, _dialogService.MessageCallCount);
        Assert.AreEqual(MessageBoxImage.Warning, _dialogService.LastIcon);
        Assert.AreEqual(0, _repository.SaveCallCount);
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

    [TestMethod]
    public async Task ImportNotesAsync_WhenUserCancelsPicker_DoesNotImport()
    {
        _filePickerService.OpenPathToReturn = null;

        await _viewModel.ImportNotesAsync();

        Assert.AreEqual(0, _backupService.ImportCallCount);
        Assert.AreEqual(0, _dialogService.MessageCallCount);
    }

    [TestMethod]
    public async Task ImportNotesAsync_OnSuccess_ImportsFromChosenPathAndShowsSuccessMessage()
    {
        var sourcePath = Path.Combine(_storageLocationProvider.BackupsDirectory, "chosen.zip");
        _filePickerService.OpenPathToReturn = sourcePath;
        _backupService.ImportCountToReturn = 3;

        await _viewModel.ImportNotesAsync();

        Assert.AreEqual(1, _backupService.ImportCallCount);
        Assert.AreEqual(sourcePath, _backupService.LastImportFilePath);
        Assert.AreEqual(1, _dialogService.MessageCallCount);
        Assert.AreEqual(MessageBoxImage.Information, _dialogService.LastIcon);
    }

    [TestMethod]
    public async Task ImportNotesAsync_OnSuccess_BroadcastsNotesImportedMessage()
    {
        _filePickerService.OpenPathToReturn = Path.Combine(_storageLocationProvider.BackupsDirectory, "chosen.zip");
        _backupService.ImportCountToReturn = 2;
        NotesImportedMessage? received = null;
        _messenger.Register<NotesImportedMessage>(this, (recipient, message) => received = message);

        await _viewModel.ImportNotesAsync();

        Assert.IsNotNull(received);
        Assert.AreEqual(2, received!.ImportedCount);
    }

    [TestMethod]
    public async Task ImportNotesAsync_WhenBackupServiceThrows_ShowsErrorMessage()
    {
        _filePickerService.OpenPathToReturn = Path.Combine(_storageLocationProvider.BackupsDirectory, "chosen.zip");
        _backupService.ImportExceptionToThrow = new InvalidDataException("not a valid backup");

        await _viewModel.ImportNotesAsync();

        Assert.AreEqual(1, _dialogService.MessageCallCount);
        Assert.AreEqual(MessageBoxImage.Error, _dialogService.LastIcon);
    }

    [TestMethod]
    public async Task CheckForUpdatesAsync_WhenUpToDate_ShowsInformationMessage()
    {
        _updateService.ResultToReturn = new UpdateCheckResult { Status = UpdateCheckStatus.UpToDate };

        await _viewModel.CheckForUpdatesAsync();

        Assert.AreEqual(1, _updateService.CheckCallCount);
        Assert.AreEqual(1, _dialogService.MessageCallCount);
        Assert.AreEqual(MessageBoxImage.Information, _dialogService.LastIcon);
        Assert.IsFalse(_viewModel.IsCheckingForUpdates);
    }

    [TestMethod]
    public async Task CheckForUpdatesAsync_WhenUpdateAvailable_ShowsInformationMessage()
    {
        _updateService.ResultToReturn = new UpdateCheckResult
        {
            Status = UpdateCheckStatus.UpdateAvailable,
            LatestVersion = "1.2.3.0"
        };

        await _viewModel.CheckForUpdatesAsync();

        Assert.AreEqual(1, _dialogService.MessageCallCount);
        Assert.AreEqual(MessageBoxImage.Information, _dialogService.LastIcon);
    }

    [TestMethod]
    public async Task CheckForUpdatesAsync_WhenCheckFails_ShowsErrorMessage()
    {
        _updateService.ResultToReturn = new UpdateCheckResult
        {
            Status = UpdateCheckStatus.Failed,
            ErrorMessage = "network unreachable"
        };

        await _viewModel.CheckForUpdatesAsync();

        Assert.AreEqual(1, _dialogService.MessageCallCount);
        Assert.AreEqual(MessageBoxImage.Error, _dialogService.LastIcon);
    }

    [TestMethod]
    public async Task CheckForUpdatesAsync_WhenServiceThrows_ShowsErrorMessageAndResetsBusyFlag()
    {
        _updateService.ExceptionToThrow = new InvalidOperationException("boom");

        await _viewModel.CheckForUpdatesAsync();

        Assert.AreEqual(1, _dialogService.MessageCallCount);
        Assert.AreEqual(MessageBoxImage.Error, _dialogService.LastIcon);
        Assert.IsFalse(_viewModel.IsCheckingForUpdates);
    }

    [TestMethod]
    public void OpenPrivacyPolicy_OpensPrivacyPolicyUrl()
    {
        _viewModel.OpenPrivacyPolicy();

        Assert.AreEqual("https://github.com/anuviswan/sticky-todo/blob/main/PRIVACY_POLICY.md", _urlLauncherService.LastUrl);
    }

    [TestMethod]
    public void OpenTermsOfService_OpensTermsOfServiceUrl()
    {
        _viewModel.OpenTermsOfService();

        Assert.AreEqual("https://github.com/anuviswan/sticky-todo/blob/main/TERMS_OF_SERVICE.md", _urlLauncherService.LastUrl);
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
        public int ImportCallCount { get; private set; }
        public string? LastImportFilePath { get; private set; }
        public int ImportCountToReturn { get; set; } = 1;
        public Exception? ImportExceptionToThrow { get; set; }

        public Task<int> ExportAsync(string filePath, string appVersion)
        {
            ExportCallCount++;
            LastFilePath = filePath;
            LastAppVersion = appVersion;

            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            return Task.FromResult(CountToReturn);
        }

        public Task<int> ImportAsync(string filePath)
        {
            ImportCallCount++;
            LastImportFilePath = filePath;

            if (ImportExceptionToThrow is not null)
                throw ImportExceptionToThrow;

            return Task.FromResult(ImportCountToReturn);
        }
    }

    private sealed class FakeFilePickerService : IFilePickerService
    {
        public string? PathToReturn { get; set; }
        public string? OpenPathToReturn { get; set; }

        public string? ShowSaveFileDialog(string defaultFileName, string filter, string? initialDirectory = null) =>
            PathToReturn;

        public string? ShowOpenFileDialog(string filter, string? initialDirectory = null) =>
            OpenPathToReturn;
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

    private sealed class FakeUrlLauncherService : IUrlLauncherService
    {
        public string? LastUrl { get; private set; }

        public void OpenUrl(string url) => LastUrl = url;
    }

    private sealed class FakeStartupTaskService : IStartupTaskService
    {
        public StartupTaskStatus StatusToReturn { get; set; } = StartupTaskStatus.Disabled;
        public StartupTaskStatus EnableResult { get; set; } = StartupTaskStatus.Enabled;
        public Exception? EnableException { get; set; }
        public Exception? DisableException { get; set; }
        public int EnableCallCount { get; private set; }
        public int DisableCallCount { get; private set; }

        public Task<StartupTaskStatus> GetStatusAsync() => Task.FromResult(StatusToReturn);

        public Task<StartupTaskStatus> EnableAsync()
        {
            EnableCallCount++;
            return EnableException is not null
                ? throw EnableException
                : Task.FromResult(EnableResult);
        }

        public Task DisableAsync()
        {
            DisableCallCount++;
            return DisableException is not null ? throw DisableException : Task.CompletedTask;
        }
    }

    private sealed class FakeUpdateService : IUpdateService
    {
        public UpdateCheckResult ResultToReturn { get; set; } = new() { Status = UpdateCheckStatus.UpToDate };
        public Exception? ExceptionToThrow { get; set; }
        public int CheckCallCount { get; private set; }

        public Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            CheckCallCount++;
            return ExceptionToThrow is not null
                ? throw ExceptionToThrow
                : Task.FromResult(ResultToReturn);
        }
    }
}
