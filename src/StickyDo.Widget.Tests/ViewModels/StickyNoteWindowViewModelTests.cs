using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Models.RichText;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Domain.Storage;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Tests.ViewModels;

[TestClass]
public class StickyNoteWindowViewModelTests
{
    private string _testDataDirectory = null!;
    private FileBasedRepository _repository = null!;
    private StickyNoteService _service = null!;
    private StickyNoteTaskService _taskService = null!;
    private StickyNoteWindowViewModel _viewModel = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "StickyDo_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);

        _repository = new FileBasedRepository(new FakeStorageLocationProvider(_testDataDirectory));
        await _repository.InitializeAsync();
        _service = new StickyNoteService(_repository);
        _taskService = new StickyNoteTaskService(_repository, _repository);
        _viewModel = new StickyNoteWindowViewModel(
            _service,
            _taskService,
            new FakeDialogService(),
            new FakeStickyNoteCreationService(),
            new FakePersistenceService(),
            new WeakReferenceMessenger(),
            new FakeWindowService());
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDataDirectory))
            Directory.Delete(_testDataDirectory, recursive: true);
    }

    [TestMethod]
    public async Task LoadNoteAsync_NoteTypeNote_DoesNotAutoSeedTask()
    {
        var noteId = await _service.CreateNoteAsync("Journal Entry", type: NoteType.Note);

        await _viewModel.LoadNoteAsync(noteId);

        Assert.AreEqual(0, _viewModel.Tasks.Count);
    }

    [TestMethod]
    public async Task LoadNoteAsync_NoteTypeNote_LoadsContentFromNote()
    {
        var noteId = await _service.CreateNoteAsync("Journal Entry", type: NoteType.Note);
        await _service.UpdateNoteAsync(noteId, "Journal Entry", StickyNoteStatus.Active, content: "Existing body text");

        await _viewModel.LoadNoteAsync(noteId);

        Assert.AreEqual("Existing body text", _viewModel.Content);
    }

    [TestMethod]
    public async Task LoadNoteAsync_NoteTypeTodo_StillAutoSeedsTaskWhenEmpty()
    {
        var noteId = await _service.CreateNoteAsync("Grocery List", type: NoteType.Todo);

        await _viewModel.LoadNoteAsync(noteId);

        Assert.AreEqual(1, _viewModel.Tasks.Count);
        Assert.AreEqual("First Task", _viewModel.Tasks[0].Title);
    }

    [TestMethod]
    public async Task CreateNewNoteAsync_CurrentNoteIsTypeNote_RequestsNoteTypeFromCreationService()
    {
        var noteId = await _service.CreateNoteAsync("Journal Entry", type: NoteType.Note);
        var creationService = new FakeStickyNoteCreationService();
        var viewModel = new StickyNoteWindowViewModel(
            _service,
            _taskService,
            new FakeDialogService(),
            creationService,
            new FakePersistenceService(),
            new WeakReferenceMessenger(),
            new FakeWindowService());
        await viewModel.LoadNoteAsync(noteId);

        await viewModel.CreateNewNoteCommand.ExecuteAsync(null);

        Assert.AreEqual(1, creationService.CallCount);
        Assert.AreEqual(NoteType.Note, creationService.LastType);
    }

    [TestMethod]
    public async Task CreateNewNoteAsync_CurrentNoteIsTypeTodo_RequestsTodoTypeFromCreationService()
    {
        var noteId = await _service.CreateNoteAsync("Grocery List", type: NoteType.Todo);
        var creationService = new FakeStickyNoteCreationService();
        var viewModel = new StickyNoteWindowViewModel(
            _service,
            _taskService,
            new FakeDialogService(),
            creationService,
            new FakePersistenceService(),
            new WeakReferenceMessenger(),
            new FakeWindowService());
        await viewModel.LoadNoteAsync(noteId);

        await viewModel.CreateNewNoteCommand.ExecuteAsync(null);

        Assert.AreEqual(1, creationService.CallCount);
        Assert.AreEqual(NoteType.Todo, creationService.LastType);
    }

    [TestMethod]
    public async Task OnContentChanged_MarksUnsavedAndTriggersAutoSave()
    {
        var noteId = await _service.CreateNoteAsync("Journal Entry", type: NoteType.Note);
        await _viewModel.LoadNoteAsync(noteId);
        var persistenceService = new FakePersistenceService();
        var viewModel = new StickyNoteWindowViewModel(
            _service,
            _taskService,
            new FakeDialogService(),
            new FakeStickyNoteCreationService(),
            persistenceService,
            new WeakReferenceMessenger(),
            new FakeWindowService());
        await viewModel.LoadNoteAsync(noteId);

        viewModel.Content = "Typed some text";

        Assert.IsTrue(persistenceService.AutoSaveStarted);
        Assert.AreEqual(NoteSaveState.NotSaved, viewModel.SaveStatus);
    }

    [TestMethod]
    public async Task SaveAsync_PersistsContent()
    {
        var noteId = await _service.CreateNoteAsync("Journal Entry", type: NoteType.Note);
        await _viewModel.LoadNoteAsync(noteId);

        _viewModel.Content = "Typed some text";
        await _viewModel.SaveCommand.ExecuteAsync(null);

        var persisted = await _service.GetNoteByIdAsync(noteId);
        Assert.AreEqual("Typed some text", persisted!.Content);
    }

    [TestMethod]
    public async Task SaveAsync_NoteWasDeletedConcurrently_ReturnsQuietlyWithoutShowingErrorDialog()
    {
        // Regression test for #134: a note can be deleted (e.g. dragged to Trash from the Notes
        // List) while its window still has unsaved edits pending. The window-closed handler then
        // calls SaveAsync, which used to throw "Note with ID {id} not found." and show a "Save
        // Error" dialog. SaveAsync should instead treat this as an expected race and no-op.
        var noteId = await _service.CreateNoteAsync("Journal Entry", type: NoteType.Note);
        var dialogService = new FakeDialogService();
        var viewModel = new StickyNoteWindowViewModel(
            _service,
            _taskService,
            dialogService,
            new FakeStickyNoteCreationService(),
            new FakePersistenceService(),
            new WeakReferenceMessenger(),
            new FakeWindowService());
        await viewModel.LoadNoteAsync(noteId);
        viewModel.Content = "Typed some text";

        await _service.DeleteNoteAsync(noteId);
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.AreEqual(0, dialogService.ShownMessages.Count);
    }

    [TestMethod]
    public async Task LoadNoteAsync_NoteTypeNote_RestoresContentFormatting()
    {
        var noteId = await _service.CreateNoteAsync("Journal Entry", type: NoteType.Note);
        var formatting = new RichTextFormatting { Spans = [new RichTextSpan { Start = 0, Length = 3, Bold = true }] };
        await _service.UpdateNoteAsync(noteId, "Journal Entry", StickyNoteStatus.Active, content: "Existing body text", contentFormatting: formatting);

        await _viewModel.LoadNoteAsync(noteId);

        Assert.IsNotNull(_viewModel.ContentFormatting);
        Assert.AreEqual(1, _viewModel.ContentFormatting.Spans.Count);
        Assert.IsTrue(_viewModel.ContentFormatting.Spans[0] is { Start: 0, Length: 3, Bold: true });
    }

    [TestMethod]
    public async Task LoadNoteAsync_LegacyNoteWithNullContentFormatting_LoadsWithoutError()
    {
        var noteId = await _service.CreateNoteAsync("Journal Entry", type: NoteType.Note);
        await _service.UpdateNoteAsync(noteId, "Journal Entry", StickyNoteStatus.Active, content: "Plain legacy text");

        await _viewModel.LoadNoteAsync(noteId);

        Assert.AreEqual("Plain legacy text", _viewModel.Content);
        Assert.IsNull(_viewModel.ContentFormatting);
    }

    [TestMethod]
    public async Task SaveAsync_PersistsContentFormatting()
    {
        var noteId = await _service.CreateNoteAsync("Journal Entry", type: NoteType.Note);
        await _viewModel.LoadNoteAsync(noteId);

        _viewModel.Content = "Typed some text";
        _viewModel.ContentFormatting = new RichTextFormatting { Spans = [new RichTextSpan { Start = 0, Length = 5, Italic = true }] };
        await _viewModel.SaveCommand.ExecuteAsync(null);

        var persisted = await _service.GetNoteByIdAsync(noteId);
        Assert.IsNotNull(persisted!.ContentFormatting);
        Assert.AreEqual(1, persisted.ContentFormatting.Spans.Count);
        Assert.IsTrue(persisted.ContentFormatting.Spans[0] is { Start: 0, Length: 5, Italic: true });
    }

    [TestMethod]
    public async Task LoadNoteAsync_NoteTypeTodo_RestoresTaskTitleFormatting()
    {
        var noteId = await _service.CreateNoteAsync("Grocery List", type: NoteType.Todo);
        var taskId = await _taskService.CreateTaskAsync(noteId, "Buy milk");
        var formatting = new RichTextFormatting { Spans = [new RichTextSpan { Start = 0, Length = 3, Bold = true }] };
        await _taskService.UpdateTaskAsync(noteId, taskId, "Buy milk", isCompleted: false, titleFormatting: formatting);

        await _viewModel.LoadNoteAsync(noteId);

        var taskVm = _viewModel.Tasks.Single(t => t.Id == taskId);
        Assert.IsNotNull(taskVm.TitleFormatting);
        Assert.AreEqual(1, taskVm.TitleFormatting.Spans.Count);
        Assert.IsTrue(taskVm.TitleFormatting.Spans[0] is { Start: 0, Length: 3, Bold: true });
    }

    [TestMethod]
    public async Task TaskItemViewModel_TitleFormattingChanged_PersistsThroughUpdateCallback()
    {
        var noteId = await _service.CreateNoteAsync("Grocery List", type: NoteType.Todo);
        var taskId = await _taskService.CreateTaskAsync(noteId, "Buy milk");
        await _viewModel.LoadNoteAsync(noteId);
        var taskVm = _viewModel.Tasks.Single(t => t.Id == taskId);

        taskVm.TitleFormatting = new RichTextFormatting { Spans = [new RichTextSpan { Start = 0, Length = 3, Underline = true }] };
        await Task.Delay(50); // OnTitleFormattingChanged fires the update fire-and-forget

        var persistedNote = await _service.GetNoteByIdAsync(noteId);
        var persistedTask = persistedNote!.Tasks.Single(t => t.Id == taskId);
        Assert.IsNotNull(persistedTask.TitleFormatting);
        Assert.IsTrue(persistedTask.TitleFormatting.Spans[0] is { Start: 0, Length: 3, Underline: true });
    }

    private sealed class FakeDialogService : IDialogService
    {
        public List<(string Title, string Message)> ShownMessages { get; } = [];

        public Task ShowMessageAsync(string title, string message, MessageBoxImage icon = MessageBoxImage.None)
        {
            ShownMessages.Add((title, message));
            return Task.CompletedTask;
        }

        public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(true);
    }

    private sealed class FakeStickyNoteCreationService : IStickyNoteCreationService
    {
        public uint? LastColorArgb { get; private set; }
        public NoteType? LastType { get; private set; }
        public int CallCount { get; private set; }

        public Task CreateNewNoteAsync(uint? colorArgb = null, NoteType type = NoteType.Todo)
        {
            LastColorArgb = colorArgb;
            LastType = type;
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWindowService : IWindowService
    {
        public void RequestMinimize() { }
        public void RequestClose() { }
        public void RequestShow() { }
    }

    private sealed class FakePersistenceService : IPersistenceService
    {
        public bool AutoSaveStarted { get; private set; }

#pragma warning disable CS0067 // required by IPersistenceService; these tests don't need to raise it
        public event EventHandler<NoteSaveStateChangedEventArgs>? NoteSaveStateChanged;
#pragma warning restore CS0067

        public void StartAutoSave() => AutoSaveStarted = true;

        public Task StopAutoSaveAsync() => Task.CompletedTask;

        public Task SaveAllDirtyNotesAsync() => Task.CompletedTask;

        public bool HasPendingChanges => false;
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
