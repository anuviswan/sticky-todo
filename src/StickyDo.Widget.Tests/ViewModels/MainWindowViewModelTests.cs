using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Tests.ViewModels;

[TestClass]
public class MainWindowViewModelTests
{
    private FileBasedRepository _repository = null!;
    private StickyNoteService _service = null!;
    private MainWindowViewModel _viewModel = null!;

    [TestInitialize]
    public async Task Setup()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var originalPath = Path.Combine(appDataPath, "StickyDo");

        if (Directory.Exists(originalPath))
        {
            var backupPath = originalPath + ".backup";
            if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, recursive: true);
            Directory.Move(originalPath, backupPath);
        }

        Directory.CreateDirectory(originalPath);

        _repository = new FileBasedRepository();
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
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var testPath = Path.Combine(appDataPath, "StickyDo");
        var backupPath = testPath + ".backup";

        if (Directory.Exists(testPath))
            Directory.Delete(testPath, recursive: true);

        if (Directory.Exists(backupPath))
            Directory.Move(backupPath, testPath);
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
}
