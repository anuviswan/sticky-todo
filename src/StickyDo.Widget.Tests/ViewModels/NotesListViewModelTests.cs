using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Tests.ViewModels;

[TestClass]
public class NotesListViewModelTests
{
    private FileBasedRepository _repository = null!;
    private StickyNoteService _service = null!;
    private NotesListViewModel _viewModel = null!;

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
        _viewModel = new NotesListViewModel(
            _service,
            new FakeStickyNoteWindowService(),
            new FakeDialogService(),
            new WeakReferenceMessenger());
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

    private async Task<Guid> CreateNoteWithTaskAsync(
        string title, string taskTitle, bool isFavorite = false, NoteType type = NoteType.Todo)
    {
        var noteId = await _service.CreateNoteAsync(title, type: type);
        var note = await _repository.GetByIdAsync(noteId);
        note!.Tasks.Add(new StickyNoteTask { Id = Guid.NewGuid(), Title = taskTitle, Order = 0 });
        note.IsFavorite = isFavorite;
        await _repository.UpdateAsync(note);
        return noteId;
    }

    private IEnumerable<StickyNoteItemViewModel> AllVisibleNotes() =>
        _viewModel.Columns.SelectMany(c => c.Notes);

    [TestMethod]
    public async Task ApplyFilter_MatchesByTitle_CaseInsensitivePartial()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk");
        await CreateNoteWithTaskAsync("Work Plan", "Finish report");
        await _viewModel.LoadNotesAsync();

        _viewModel.SearchQuery = "groc";

        Assert.AreEqual(1, AllVisibleNotes().Count());
        Assert.AreEqual("Grocery List", AllVisibleNotes().Single().Title);
    }

    [TestMethod]
    public async Task ApplyFilter_MatchesByTaskTitle_Content()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk");
        await CreateNoteWithTaskAsync("Work Plan", "Finish report");
        await _viewModel.LoadNotesAsync();

        _viewModel.SearchQuery = "REPORT";

        Assert.AreEqual(1, AllVisibleNotes().Count());
        Assert.AreEqual("Work Plan", AllVisibleNotes().Single().Title);
    }

    [TestMethod]
    public async Task ApplyFilter_TrimsLeadingAndTrailingWhitespace()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk");
        await _viewModel.LoadNotesAsync();

        _viewModel.SearchQuery = "  grocery  ";

        Assert.AreEqual(1, AllVisibleNotes().Count());
    }

    [TestMethod]
    public async Task ApplyFilter_ComposesWithShowFavoritesOnly()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk", isFavorite: true);
        await CreateNoteWithTaskAsync("Grocery Run", "Buy eggs", isFavorite: false);
        await _viewModel.LoadNotesAsync();

        _viewModel.ShowFavoritesOnly = true;
        _viewModel.SearchQuery = "grocery";

        Assert.AreEqual(1, AllVisibleNotes().Count());
        Assert.AreEqual("Grocery List", AllVisibleNotes().Single().Title);
    }

    [TestMethod]
    public async Task ApplyFilter_ClearingQuery_RestoresFullList()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk");
        await CreateNoteWithTaskAsync("Work Plan", "Finish report");
        await _viewModel.LoadNotesAsync();

        _viewModel.SearchQuery = "grocery";
        _viewModel.SearchQuery = string.Empty;

        Assert.AreEqual(2, AllVisibleNotes().Count());
    }

    [TestMethod]
    public async Task ApplyFilter_NoMatches_ColumnsIsEmpty()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk");
        await _viewModel.LoadNotesAsync();

        _viewModel.SearchQuery = "nonexistent-term";

        Assert.AreEqual(0, AllVisibleNotes().Count());
        Assert.IsTrue(_viewModel.IsSearchActive);
    }

    [TestMethod]
    public async Task IsSearchActive_ReflectsWhitespaceOnlyQueryAsInactive()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk");
        await _viewModel.LoadNotesAsync();

        _viewModel.SearchQuery = "   ";

        Assert.IsFalse(_viewModel.IsSearchActive);
        Assert.AreEqual(1, AllVisibleNotes().Count());
    }

    [TestMethod]
    public async Task ApplyFilter_TypeFilterTodo_ReturnsOnlyTodoNotes()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk", type: NoteType.Todo);
        await CreateNoteWithTaskAsync("Journal Entry", "n/a", type: NoteType.Note);
        await _viewModel.LoadNotesAsync();

        _viewModel.TypeFilter = NoteType.Todo;

        Assert.AreEqual(1, AllVisibleNotes().Count());
        Assert.AreEqual("Grocery List", AllVisibleNotes().Single().Title);
    }

    [TestMethod]
    public async Task ApplyFilter_TypeFilterNote_ReturnsOnlyNoteNotes()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk", type: NoteType.Todo);
        await CreateNoteWithTaskAsync("Journal Entry", "n/a", type: NoteType.Note);
        await _viewModel.LoadNotesAsync();

        _viewModel.TypeFilter = NoteType.Note;

        Assert.AreEqual(1, AllVisibleNotes().Count());
        Assert.AreEqual("Journal Entry", AllVisibleNotes().Single().Title);
    }

    [TestMethod]
    public async Task ApplyFilter_TypeFilterNull_ReturnsAllNotes()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk", type: NoteType.Todo);
        await CreateNoteWithTaskAsync("Journal Entry", "n/a", type: NoteType.Note);
        await _viewModel.LoadNotesAsync();

        _viewModel.TypeFilter = NoteType.Todo;
        _viewModel.TypeFilter = null;

        Assert.AreEqual(2, AllVisibleNotes().Count());
    }

    [TestMethod]
    public async Task ApplyFilter_ComposesTypeFilterWithSearch()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk", type: NoteType.Todo);
        await CreateNoteWithTaskAsync("Grocery Notes", "n/a", type: NoteType.Note);
        await _viewModel.LoadNotesAsync();

        _viewModel.TypeFilter = NoteType.Note;
        _viewModel.SearchQuery = "grocery";

        Assert.AreEqual(1, AllVisibleNotes().Count());
        Assert.AreEqual("Grocery Notes", AllVisibleNotes().Single().Title);
    }

    private sealed class FakeStickyNoteWindowService : IStickyNoteWindowService
    {
        public Task OpenNoteWindowAsync(Guid noteId) => Task.CompletedTask;
    }

    private sealed class FakeDialogService : IDialogService
    {
        public Task ShowMessageAsync(string title, string message, MessageBoxImage icon = MessageBoxImage.None) =>
            Task.CompletedTask;

        public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(true);
    }
}
