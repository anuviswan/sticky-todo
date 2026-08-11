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
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Tests.ViewModels;

[TestClass]
public class NotesListViewModelTests
{
    private string _testDataDirectory = null!;
    private FileBasedRepository _repository = null!;
    private StickyNoteService _service = null!;
    private FakeSettingsRepository _settingsRepository = null!;
    private NotesListViewModel _viewModel = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "StickyDo_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);

        _repository = new FileBasedRepository(new FakeStorageLocationProvider(_testDataDirectory));
        await _repository.InitializeAsync();
        _service = new StickyNoteService(_repository);
        _settingsRepository = new FakeSettingsRepository();
        _viewModel = new NotesListViewModel(
            _service,
            new FakeStickyNoteWindowService(),
            new FakeDialogService(),
            new WeakReferenceMessenger(),
            _settingsRepository,
            new PersistenceService(_repository));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDataDirectory))
            Directory.Delete(_testDataDirectory, recursive: true);
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

    private async Task<Guid> CreateNoteWithContentAsync(string title, string content)
    {
        var noteId = await _service.CreateNoteAsync(title, type: NoteType.Note);
        await _service.UpdateNoteAsync(noteId, title, StickyNoteStatus.Active, content: content);
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
    public async Task ApplyFilter_MatchesByNoteContent()
    {
        await CreateNoteWithContentAsync("Journal Entry", "Remember to water the plants");
        await CreateNoteWithContentAsync("Trip Notes", "Pack sunscreen and a hat");
        await _viewModel.LoadNotesAsync();

        _viewModel.SearchQuery = "PLANTS";

        Assert.AreEqual(1, AllVisibleNotes().Count());
        Assert.AreEqual("Journal Entry", AllVisibleNotes().Single().Title);
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

    [TestMethod]
    public async Task CreateNoteAsync_WhenTypeFilterIsNote_CreatesNoteTypeItem()
    {
        _viewModel.TypeFilter = NoteType.Note;

        await _viewModel.CreateNoteAsync();

        var created = AllVisibleNotes().Single();
        Assert.AreEqual(NoteType.Note, created.Type);

        var persisted = await _service.GetNoteByIdAsync(created.Id);
        Assert.AreEqual(NoteType.Note, persisted!.Type);
    }

    [TestMethod]
    public async Task CreateNoteAsync_WhenTypeFilterIsTodoOrNull_CreatesTodoTypeItem()
    {
        _viewModel.TypeFilter = NoteType.Todo;
        await _viewModel.CreateNoteAsync();

        _viewModel.TypeFilter = null;
        await _viewModel.CreateNoteAsync();

        _viewModel.TypeFilter = null;
        Assert.AreEqual(2, AllVisibleNotes().Count());
        Assert.IsTrue(AllVisibleNotes().All(n => n.Type == NoteType.Todo));
    }

    [TestMethod]
    public async Task CreateNoteAsync_UsesPersistedDefaultNoteColor()
    {
        var color = ColorPalette.Colors[3];
        _settingsRepository.Settings = new AppSettings { DefaultNoteColor = color };

        await _viewModel.CreateNoteAsync();

        var created = AllVisibleNotes().Single();
        Assert.AreEqual(color, created.ColorArgb);

        var persisted = await _service.GetNoteByIdAsync(created.Id);
        Assert.AreEqual(color, persisted!.ColorArgb);
    }

    [TestMethod]
    public async Task FirstTaskTitle_ShortTitle_ShowsFullText()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk");
        await _viewModel.LoadNotesAsync();

        Assert.AreEqual("Buy milk", AllVisibleNotes().Single().FirstTaskTitle);
    }

    [TestMethod]
    public async Task FirstTaskTitle_LongTitle_TruncatesToFirstWordsWithEllipsis()
    {
        var taskTitle = string.Join(' ', Enumerable.Range(1, 20).Select(i => $"word{i}"));
        await CreateNoteWithTaskAsync("Grocery List", taskTitle);
        await _viewModel.LoadNotesAsync();

        var expected = string.Join(' ', Enumerable.Range(1, 12).Select(i => $"word{i}")) + "...";
        Assert.AreEqual(expected, AllVisibleNotes().Single().FirstTaskTitle);
    }

    [TestMethod]
    public async Task ContentPreview_ShortContent_ShowsFullText()
    {
        await CreateNoteWithContentAsync("Journal Entry", "Buy milk and eggs");
        await _viewModel.LoadNotesAsync();

        Assert.AreEqual("Buy milk and eggs", AllVisibleNotes().Single().ContentPreview);
    }

    [TestMethod]
    public async Task ContentPreview_LongContent_TruncatesToFirstWordsWithEllipsis()
    {
        var content = string.Join(' ', Enumerable.Range(1, 20).Select(i => $"word{i}"));
        await CreateNoteWithContentAsync("Journal Entry", content);
        await _viewModel.LoadNotesAsync();

        var expected = string.Join(' ', Enumerable.Range(1, 12).Select(i => $"word{i}")) + "...";
        Assert.AreEqual(expected, AllVisibleNotes().Single().ContentPreview);
    }

    [TestMethod]
    public async Task ContentPreview_MultiLineContent_CollapsesNewlinesToSingleLine()
    {
        await CreateNoteWithContentAsync("Journal Entry", "Line one\nLine two\nLine three");
        await _viewModel.LoadNotesAsync();

        Assert.AreEqual("Line one Line two Line three", AllVisibleNotes().Single().ContentPreview);
    }

    [TestMethod]
    public async Task ContentPreview_TodoNoteWithNoContent_IsEmpty()
    {
        await CreateNoteWithTaskAsync("Grocery List", "Buy milk");
        await _viewModel.LoadNotesAsync();

        Assert.AreEqual(string.Empty, AllVisibleNotes().Single().ContentPreview);
    }

    [TestMethod]
    public async Task ToggleFavoriteAsync_TwoNotesInSequence_BothPersistToDisk()
    {
        // Regression test for issue #136: favouriting a note from the Notes List only marked it
        // dirty in memory - without a note window open to trigger an incidental autosave, nothing
        // ever flushed it to disk, so an earlier favourite could appear lost.
        var noteId1 = await CreateNoteWithTaskAsync("Grocery List", "Buy milk");
        var noteId2 = await CreateNoteWithTaskAsync("Work Plan", "Finish report");
        await _viewModel.LoadNotesAsync();

        await _viewModel.ToggleFavoriteAsync(noteId1);
        await _viewModel.ToggleFavoriteAsync(noteId2);

        var reloadedRepository = new FileBasedRepository(new FakeStorageLocationProvider(_testDataDirectory));
        await reloadedRepository.InitializeAsync();

        Assert.IsTrue((await reloadedRepository.GetByIdAsync(noteId1))!.IsFavorite);
        Assert.IsTrue((await reloadedRepository.GetByIdAsync(noteId2))!.IsFavorite);
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

    private sealed class FakeSettingsRepository : ISettingsRepository
    {
        public AppSettings Settings { get; set; } = new();

        public Task<AppSettings> LoadAsync() => Task.FromResult(Settings);

        public Task SaveAsync(AppSettings settings)
        {
            Settings = settings;
            return Task.CompletedTask;
        }
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
