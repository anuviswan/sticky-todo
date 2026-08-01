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
public class StickyNoteWindowViewModelTests
{
    private FileBasedRepository _repository = null!;
    private StickyNoteService _service = null!;
    private StickyNoteTaskService _taskService = null!;
    private StickyNoteWindowViewModel _viewModel = null!;

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
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var testPath = Path.Combine(appDataPath, "StickyDo");
        var backupPath = testPath + ".backup";

        if (Directory.Exists(testPath))
            Directory.Delete(testPath, recursive: true);

        if (Directory.Exists(backupPath))
            Directory.Move(backupPath, testPath);
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

    private sealed class FakeDialogService : IDialogService
    {
        public Task ShowMessageAsync(string title, string message, MessageBoxImage icon = MessageBoxImage.None) =>
            Task.CompletedTask;

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

        public event EventHandler<NoteSaveStateChangedEventArgs>? NoteSaveStateChanged;

        public void StartAutoSave() => AutoSaveStarted = true;

        public Task StopAutoSaveAsync() => Task.CompletedTask;

        public Task SaveAllDirtyNotesAsync() => Task.CompletedTask;

        public bool HasPendingChanges => false;
    }
}
