using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Domain.Storage;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Tests.ViewModels;

[TestClass]
public class MainWindowViewModelTests
{
    private string _testDataDirectory = null!;
    private FileBasedRepository _repository = null!;
    private StickyNoteService _service = null!;
    private FakeStartupTaskService _startupTaskService = null!;
    private MainWindowViewModel _viewModel = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "StickyDo_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);

        _repository = new FileBasedRepository(new FakeStorageLocationProvider(_testDataDirectory));
        await _repository.InitializeAsync();
        _service = new StickyNoteService(_repository);
        var notesListViewModel = new NotesListViewModel(
            _service,
            new FakeStickyNoteWindowService(),
            new FakeDialogService(),
            new WeakReferenceMessenger(),
            new FakeSettingsRepository());
        _startupTaskService = new FakeStartupTaskService();
        var settingsViewModel = new SettingsViewModel(
            new FakeSettingsRepository(),
            new FakeBackupService(),
            new FakeFilePickerService(),
            new FakeDialogService(),
            new FakeStorageLocationProvider(_testDataDirectory),
            _repository,
            new WeakReferenceMessenger(),
            new FakeUrlLauncherService(),
            _startupTaskService,
            new FakeUpdateService());
        _viewModel = new MainWindowViewModel(new FakeWindowService(), notesListViewModel, settingsViewModel);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDataDirectory))
            Directory.Delete(_testDataDirectory, recursive: true);
    }

    [TestMethod]
    public void ShowTodos_SetsSelectedNavView_AndTypeFilter()
    {
        _viewModel.ShowTodos();

        Assert.AreEqual(NavigationView.Todos, _viewModel.SelectedNavView);
        Assert.AreEqual(NoteType.Todo, _viewModel.NotesListViewModel.TypeFilter);
        Assert.IsFalse(_viewModel.NotesListViewModel.ShowFavoritesOnly);
    }

    [TestMethod]
    public void ShowNotes_SetsSelectedNavView_AndTypeFilter()
    {
        _viewModel.ShowNotes();

        Assert.AreEqual(NavigationView.Notes, _viewModel.SelectedNavView);
        Assert.AreEqual(NoteType.Note, _viewModel.NotesListViewModel.TypeFilter);
        Assert.IsFalse(_viewModel.NotesListViewModel.ShowFavoritesOnly);
    }

    [TestMethod]
    public void Constructor_DefaultsToUnfilteredViewWithNoNavIconSelected()
    {
        Assert.AreEqual(NavigationView.AllNotes, _viewModel.SelectedNavView);
        Assert.IsNull(_viewModel.NotesListViewModel.TypeFilter);
        Assert.IsFalse(_viewModel.NotesListViewModel.ShowFavoritesOnly);
    }

    [TestMethod]
    public async Task LoadNotesAsync_WithMoreTodos_DefaultsToTodos()
    {
        await _service.CreateNoteAsync("Todo 1", type: NoteType.Todo);
        await _service.CreateNoteAsync("Todo 2", type: NoteType.Todo);
        await _service.CreateNoteAsync("Note 1", type: NoteType.Note);

        await _viewModel.LoadNotesAsync();

        Assert.AreEqual(NavigationView.Todos, _viewModel.SelectedNavView);
        Assert.AreEqual(NoteType.Todo, _viewModel.NotesListViewModel.TypeFilter);
    }

    [TestMethod]
    public async Task LoadNotesAsync_WithMoreNotes_DefaultsToNotes()
    {
        await _service.CreateNoteAsync("Todo 1", type: NoteType.Todo);
        await _service.CreateNoteAsync("Note 1", type: NoteType.Note);
        await _service.CreateNoteAsync("Note 2", type: NoteType.Note);

        await _viewModel.LoadNotesAsync();

        Assert.AreEqual(NavigationView.Notes, _viewModel.SelectedNavView);
        Assert.AreEqual(NoteType.Note, _viewModel.NotesListViewModel.TypeFilter);
    }

    [TestMethod]
    public async Task LoadNotesAsync_WithEqualCountsOrNoData_DefaultsToTodos()
    {
        await _service.CreateNoteAsync("Todo 1", type: NoteType.Todo);
        await _service.CreateNoteAsync("Note 1", type: NoteType.Note);

        await _viewModel.LoadNotesAsync();

        Assert.AreEqual(NavigationView.Todos, _viewModel.SelectedNavView);
    }

    [TestMethod]
    public async Task LoadNotesAsync_DoesNotOverrideAnAlreadyChosenNavView()
    {
        await _service.CreateNoteAsync("Note 1", type: NoteType.Note);
        await _service.CreateNoteAsync("Note 2", type: NoteType.Note);
        _viewModel.ShowFavorites();

        await _viewModel.LoadNotesAsync();

        Assert.AreEqual(NavigationView.Favorites, _viewModel.SelectedNavView);
    }

    [TestMethod]
    public void ShowFavorites_ClearsTypeFilter()
    {
        _viewModel.ShowNotes();

        _viewModel.ShowFavorites();

        Assert.AreEqual(NavigationView.Favorites, _viewModel.SelectedNavView);
        Assert.IsNull(_viewModel.NotesListViewModel.TypeFilter);
        Assert.IsTrue(_viewModel.NotesListViewModel.ShowFavoritesOnly);
    }

    [TestMethod]
    public async Task ShowTodos_ClosesSettings()
    {
        await _viewModel.OpenSettingsAsync();

        _viewModel.ShowTodos();

        Assert.IsFalse(_viewModel.IsSettingsOpen);
    }

    [TestMethod]
    public async Task ShowNotes_ClosesSettings()
    {
        await _viewModel.OpenSettingsAsync();

        _viewModel.ShowNotes();

        Assert.IsFalse(_viewModel.IsSettingsOpen);
    }

    [TestMethod]
    public async Task ShowFavorites_ClosesSettings()
    {
        await _viewModel.OpenSettingsAsync();

        _viewModel.ShowFavorites();

        Assert.IsFalse(_viewModel.IsSettingsOpen);
    }

    [TestMethod]
    public async Task OpenSettingsAsync_SetsIsSettingsOpen()
    {
        await _viewModel.OpenSettingsAsync();

        Assert.IsTrue(_viewModel.IsSettingsOpen);
    }

    [TestMethod]
    public async Task SettingsCloseRequested_ClearsIsSettingsOpen()
    {
        await _viewModel.OpenSettingsAsync();

        _viewModel.Settings.Close();

        Assert.IsFalse(_viewModel.IsSettingsOpen);
    }

    [TestMethod]
    public async Task OpenSettingsAsync_RefreshesStartupState()
    {
        _startupTaskService.StatusToReturn = StartupTaskStatus.Enabled;

        await _viewModel.OpenSettingsAsync();

        Assert.IsTrue(_viewModel.Settings.LaunchAtStartup);
    }

    private sealed class FakeWindowService : IWindowService
    {
        public void RequestMinimize() { }

        public void RequestClose() { }

        public void RequestShow() { }
    }

    private sealed class FakeStickyNoteWindowService : IStickyNoteWindowService
    {
        public Task OpenNoteWindowAsync(Guid noteId) => Task.CompletedTask;
    }

    private sealed class FakeDialogService : IDialogService
    {
        public Task ShowMessageAsync(string title, string message, System.Windows.MessageBoxImage icon = System.Windows.MessageBoxImage.None) =>
            Task.CompletedTask;

        public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(true);
    }

    private sealed class FakeSettingsRepository : ISettingsRepository
    {
        public Task<AppSettings> LoadAsync() => Task.FromResult(new AppSettings());

        public Task SaveAsync(AppSettings settings) => Task.CompletedTask;
    }

    private sealed class FakeBackupService : IBackupService
    {
        public Task<int> ExportAsync(string filePath, string appVersion) => Task.FromResult(0);

        public Task<int> ImportAsync(string filePath) => Task.FromResult(0);
    }

    private sealed class FakeFilePickerService : IFilePickerService
    {
        public string? ShowSaveFileDialog(string defaultFileName, string filter, string? initialDirectory = null) => null;

        public string? ShowOpenFileDialog(string filter, string? initialDirectory = null) => null;
    }

    private sealed class FakeUrlLauncherService : IUrlLauncherService
    {
        public void OpenUrl(string url) { }
    }

    private sealed class FakeStartupTaskService : IStartupTaskService
    {
        public StartupTaskStatus StatusToReturn { get; set; } = StartupTaskStatus.Disabled;

        public Task<StartupTaskStatus> GetStatusAsync() => Task.FromResult(StatusToReturn);

        public Task<StartupTaskStatus> EnableAsync() => Task.FromResult(StartupTaskStatus.Enabled);

        public Task DisableAsync() => Task.CompletedTask;
    }

    private sealed class FakeUpdateService : IUpdateService
    {
        public Task<UpdateCheckResult> CheckForUpdatesAsync() =>
            Task.FromResult(new UpdateCheckResult { Status = UpdateCheckStatus.UpToDate });
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
