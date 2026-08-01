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
            new WeakReferenceMessenger());
        _viewModel = new MainWindowViewModel(new FakeWindowService(), notesListViewModel);
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
    public void ShowAllNotes_ClearsTypeFilterAndFavoritesOnly()
    {
        _viewModel.ShowTodos();
        _viewModel.ShowFavorites();

        _viewModel.ShowAllNotes();

        Assert.AreEqual(NavigationView.AllNotes, _viewModel.SelectedNavView);
        Assert.IsNull(_viewModel.NotesListViewModel.TypeFilter);
        Assert.IsFalse(_viewModel.NotesListViewModel.ShowFavoritesOnly);
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
