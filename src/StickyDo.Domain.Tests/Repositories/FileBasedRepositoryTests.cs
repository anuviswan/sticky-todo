using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;

namespace StickyDo.Domain.Tests.Repositories;

[TestClass]
public class FileBasedRepositoryTests
{
    private string _testDataDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "StickyTODO_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);

        // Redirect the data directory for testing
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var originalPath = Path.Combine(appDataPath, "StickyTODO");

        // Back up original if it exists
        if (Directory.Exists(originalPath))
        {
            var backupPath = originalPath + ".backup";
            if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, recursive: true);
            Directory.Move(originalPath, backupPath);
        }

        // Create test directory in LocalAppData
        Directory.CreateDirectory(Path.Combine(appDataPath, "StickyTODO"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var testPath = Path.Combine(appDataPath, "StickyTODO");
        var backupPath = testPath + ".backup";

        if (Directory.Exists(testPath))
            Directory.Delete(testPath, recursive: true);

        if (Directory.Exists(backupPath))
            Directory.Move(backupPath, testPath);

        if (Directory.Exists(_testDataDirectory))
            Directory.Delete(_testDataDirectory, recursive: true);
    }

    [TestMethod]
    public async Task InitializeAsync_LoadsNotesFromDisk()
    {
        // Arrange
        var repository = new FileBasedRepository();

        // Act
        await repository.InitializeAsync();

        // Assert - should initialize with sample data on first run
        var notes = await repository.GetAllAsync();
        Assert.IsTrue(notes.Count() > 0);
    }

    [TestMethod]
    public async Task CreateAsync_AddsNoteAndMarksDirty()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        var note = new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = "Test Note",
            Status = StickyNoteStatus.Active
        };

        // Act
        var id = await repository.CreateAsync(note);

        // Assert
        Assert.AreEqual(note.Id, id);
        var retrieved = await repository.GetByIdAsync(id);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Test Note", retrieved.Title);
        Assert.IsTrue(repository.GetDirtyNotes().Contains(id));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task CreateAsync_ThrowsOnNullNote()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        // Act
        await repository.CreateAsync(null!);
    }

    [TestMethod]
    public async Task UpdateAsync_ModifiesNoteAndMarksDirty()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        var note = new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = "Original",
            Status = StickyNoteStatus.Active
        };
        await repository.CreateAsync(note);

        var updatedNote = new StickyNote
        {
            Id = note.Id,
            Title = "Updated",
            Status = StickyNoteStatus.Completed
        };

        // Act
        await repository.UpdateAsync(updatedNote);

        // Assert
        var retrieved = await repository.GetByIdAsync(note.Id);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Updated", retrieved.Title);
        Assert.IsTrue(repository.GetDirtyNotes().Contains(note.Id));
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesNoteAndMarksDirty()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        var note = new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = "To Delete",
            Status = StickyNoteStatus.Active
        };
        await repository.CreateAsync(note);

        // Act
        await repository.DeleteAsync(note.Id);

        // Assert
        var retrieved = await repository.GetByIdAsync(note.Id);
        Assert.IsNull(retrieved);
        Assert.IsTrue(repository.GetDirtyNotes().Contains(note.Id));
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsAllNotes()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        var note1 = new StickyNote { Id = Guid.NewGuid(), Title = "Note 1" };
        var note2 = new StickyNote { Id = Guid.NewGuid(), Title = "Note 2" };

        await repository.CreateAsync(note1);
        await repository.CreateAsync(note2);

        // Act
        var notes = await repository.GetAllAsync();

        // Assert
        Assert.IsTrue(notes.Count() >= 2);
    }

    [TestMethod]
    public async Task SearchAsync_FindsNotesByTitle()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "Unique Search Term" };
        await repository.CreateAsync(note);

        // Act
        var results = await repository.SearchAsync("Search");

        // Assert
        Assert.IsTrue(results.Any(n => n.Id == note.Id));
    }

    [TestMethod]
    public async Task GetByStatusAsync_FiltersNotesByStatus()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        var activeNote = new StickyNote { Id = Guid.NewGuid(), Title = "Active", Status = StickyNoteStatus.Active };
        await repository.CreateAsync(activeNote);

        // Act
        var active = await repository.GetByStatusAsync(StickyNoteStatus.Active);

        // Assert
        Assert.IsTrue(active.Any(n => n.Id == activeNote.Id));
    }

    [TestMethod]
    public async Task CreateTask_AddsTaskToNote()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "Note with Task" };
        await repository.CreateAsync(note);

        var task = new StickyNoteTask { Id = Guid.NewGuid(), Title = "Test Task" };

        // Act
        var taskId = await ((IStickyNoteTaskRepository)repository).CreateAsync(note.Id, task);

        // Assert
        Assert.AreEqual(task.Id, taskId);
        var retrieved = await repository.GetByIdAsync(note.Id);
        Assert.AreEqual(1, retrieved!.Tasks.Count);
        Assert.IsTrue(repository.GetDirtyNotes().Contains(note.Id));
    }

    [TestMethod]
    public async Task UpdateTask_ModifiesTaskInNote()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "Note" };
        await repository.CreateAsync(note);

        var task = new StickyNoteTask { Id = Guid.NewGuid(), Title = "Original" };
        await ((IStickyNoteTaskRepository)repository).CreateAsync(note.Id, task);

        var updatedTask = new StickyNoteTask { Id = task.Id, Title = "Updated", IsCompleted = true };

        // Act
        await ((IStickyNoteTaskRepository)repository).UpdateAsync(note.Id, updatedTask);

        // Assert
        var retrieved = await repository.GetByIdAsync(note.Id);
        var retrievedTask = retrieved!.Tasks.First(t => t.Id == task.Id);
        Assert.AreEqual("Updated", retrievedTask.Title);
        Assert.IsTrue(retrievedTask.IsCompleted);
    }

    [TestMethod]
    public async Task DeleteTask_RemovesTaskFromNote()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "Note" };
        await repository.CreateAsync(note);

        var task = new StickyNoteTask { Id = Guid.NewGuid(), Title = "To Delete" };
        await ((IStickyNoteTaskRepository)repository).CreateAsync(note.Id, task);

        // Act
        await ((IStickyNoteTaskRepository)repository).DeleteAsync(note.Id, task.Id);

        // Assert
        var retrieved = await repository.GetByIdAsync(note.Id);
        Assert.AreEqual(0, retrieved!.Tasks.Count);
    }

    [TestMethod]
    public async Task SaveNoteAsync_WriteNoteToFile()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "To Save" };
        await repository.CreateAsync(note);

        // Act
        await repository.SaveNoteAsync(note.Id);

        // Assert
        Assert.IsFalse(repository.GetDirtyNotes().Contains(note.Id));
    }

    [TestMethod]
    public async Task SaveAllDirtyNotesAsync_SavesAllModifiedNotes()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        var note1 = new StickyNote { Id = Guid.NewGuid(), Title = "Note 1" };
        var note2 = new StickyNote { Id = Guid.NewGuid(), Title = "Note 2" };

        await repository.CreateAsync(note1);
        await repository.CreateAsync(note2);

        // Act
        await repository.SaveAllDirtyNotesAsync();

        // Assert
        Assert.AreEqual(0, repository.GetDirtyNotes().Count());
    }

    [TestMethod]
    public async Task HasPendingChanges_ReflectsDirtyState()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        // Act & Assert
        Assert.IsFalse(repository.HasPendingChanges);

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "Pending" };
        await repository.CreateAsync(note);

        Assert.IsTrue(repository.HasPendingChanges);

        await repository.SaveAllDirtyNotesAsync();

        Assert.IsFalse(repository.HasPendingChanges);
    }

    [TestMethod]
    public async Task GetByIdAsync_ReturnsNullForNonExistentNote()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.IsNull(result);
    }
}
