using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;

namespace StickyDo.Domain.Tests.Services;

[TestClass]
public class StickyNoteTaskServiceTests
{
    [TestInitialize]
    public void Setup()
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

    private static async Task<(StickyNoteService NoteService, StickyNoteTaskService TaskService, Guid NoteId)> CreateTodoWithTasksAsync(
        params string[] taskTitles)
    {
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var noteService = new StickyNoteService(repository);
        var taskService = new StickyNoteTaskService(repository, repository);

        var noteId = await noteService.CreateNoteAsync("Test Note", type: NoteType.Todo);
        foreach (var title in taskTitles)
            await taskService.CreateTaskAsync(noteId, title);

        return (noteService, taskService, noteId);
    }

    [TestMethod]
    public async Task ConvertNoteTypeAsync_TodoToNote_JoinsTaskTitlesIntoContent()
    {
        var (_, taskService, noteId) = await CreateTodoWithTasksAsync("Buy milk", "Walk dog");

        var converted = await taskService.ConvertNoteTypeAsync(noteId, NoteType.Note);

        Assert.AreEqual(NoteType.Note, converted.Type);
        Assert.AreEqual($"Buy milk{Environment.NewLine}Walk dog", converted.Content);
    }

    [TestMethod]
    public async Task ConvertNoteTypeAsync_TodoToNote_ClearsTasks()
    {
        var (_, taskService, noteId) = await CreateTodoWithTasksAsync("Buy milk", "Walk dog");

        var converted = await taskService.ConvertNoteTypeAsync(noteId, NoteType.Note);

        Assert.AreEqual(0, converted.Tasks.Count);
    }

    [TestMethod]
    public async Task ConvertNoteTypeAsync_NoteToTodo_CreatesOneTaskPerNonEmptyLine()
    {
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var noteService = new StickyNoteService(repository);
        var taskService = new StickyNoteTaskService(repository, repository);
        var noteId = await noteService.CreateNoteAsync("Journal", type: NoteType.Note);
        await noteService.UpdateNoteAsync(noteId, "Journal", StickyNoteStatus.Active, content: "Buy milk\n\nWalk dog");

        var converted = await taskService.ConvertNoteTypeAsync(noteId, NoteType.Todo);

        Assert.AreEqual(NoteType.Todo, converted.Type);
        Assert.AreEqual(2, converted.Tasks.Count);
        var ordered = converted.Tasks.OrderBy(t => t.Order).ToList();
        Assert.AreEqual("Buy milk", ordered[0].Title);
        Assert.AreEqual("Walk dog", ordered[1].Title);
        Assert.IsFalse(ordered[0].IsCompleted);
        Assert.IsFalse(ordered[1].IsCompleted);
    }

    [TestMethod]
    public async Task ConvertNoteTypeAsync_NoteToTodo_ClearsContent()
    {
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var noteService = new StickyNoteService(repository);
        var taskService = new StickyNoteTaskService(repository, repository);
        var noteId = await noteService.CreateNoteAsync("Journal", type: NoteType.Note);
        await noteService.UpdateNoteAsync(noteId, "Journal", StickyNoteStatus.Active, content: "Buy milk");

        var converted = await taskService.ConvertNoteTypeAsync(noteId, NoteType.Todo);

        Assert.IsNull(converted.Content);
    }

    [TestMethod]
    public async Task ConvertNoteTypeAsync_PreservesIdColorFavoriteAndPinnedState()
    {
        var (noteService, taskService, noteId) = await CreateTodoWithTasksAsync("Buy milk");
        await noteService.UpdateNoteAsync(noteId, "Test Note", StickyNoteStatus.Active, color: 0xFFAABBCC);
        await noteService.SetNoteFavoriteAsync(noteId, true);
        await noteService.SetNotePinnedAsync(noteId, true);

        var converted = await taskService.ConvertNoteTypeAsync(noteId, NoteType.Note);

        Assert.AreEqual(noteId, converted.Id);
        Assert.AreEqual((uint)0xFFAABBCC, converted.ColorArgb);
        Assert.IsTrue(converted.IsFavorite);
        Assert.IsTrue(converted.IsPinned);
    }

    [TestMethod]
    public async Task ConvertNoteTypeAsync_SameType_IsNoOp()
    {
        var (_, taskService, noteId) = await CreateTodoWithTasksAsync("Buy milk");

        var converted = await taskService.ConvertNoteTypeAsync(noteId, NoteType.Todo);

        Assert.AreEqual(1, converted.Tasks.Count);
        Assert.AreEqual("Buy milk", converted.Tasks[0].Title);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task ConvertNoteTypeAsync_ThrowsOnEmptyId()
    {
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var taskService = new StickyNoteTaskService(repository, repository);

        await taskService.ConvertNoteTypeAsync(Guid.Empty, NoteType.Note);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task ConvertNoteTypeAsync_ThrowsWhenNoteNotFound()
    {
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var taskService = new StickyNoteTaskService(repository, repository);

        await taskService.ConvertNoteTypeAsync(Guid.NewGuid(), NoteType.Note);
    }
}
