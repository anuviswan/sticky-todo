using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Models.RichText;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Storage;
using StickyDo.Domain.Utilities;

namespace StickyDo.Domain.Tests.Repositories;

[TestClass]
public class FileBasedRepositoryTests
{
    private string _testDataDirectory = null!;
    private IStorageLocationProvider _storageLocationProvider = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "StickyDo_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);
        _storageLocationProvider = new FakeStorageLocationProvider(_testDataDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDataDirectory))
            Directory.Delete(_testDataDirectory, recursive: true);
    }

    [TestMethod]
    public async Task InitializeAsync_LoadsNotesFromDisk()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);

        // Act
        await repository.InitializeAsync();

        // Assert - should start empty on first run (no sample data)
        var notes = await repository.GetAllAsync();
        Assert.AreEqual(0, notes.Count());
    }

    [TestMethod]
    public async Task CreateAsync_AddsNoteAndMarksDirty()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
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
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();

        // Act
        await repository.CreateAsync(null!);
    }

    [TestMethod]
    public async Task UpdateAsync_ModifiesNoteAndMarksDirty()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
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
    public async Task UpdateAsync_ModifiesTypeAndMarksDirty()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();

        var note = new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = "Original",
            Status = StickyNoteStatus.Active,
            Type = NoteType.Todo
        };
        await repository.CreateAsync(note);

        var updatedNote = new StickyNote
        {
            Id = note.Id,
            Title = "Original",
            Status = StickyNoteStatus.Active,
            Type = NoteType.Note
        };

        // Act
        await repository.UpdateAsync(updatedNote);

        // Assert
        var retrieved = await repository.GetByIdAsync(note.Id);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(NoteType.Note, retrieved.Type);
        Assert.IsTrue(repository.GetDirtyNotes().Contains(note.Id));
    }

    [TestMethod]
    public async Task LoadAllNotesFromDiskAsync_MissingTypeProperty_DefaultsToTodo()
    {
        // Arrange - write a raw JSON file without a "Type" property, simulating a note
        // saved before this field existed.
        var pathHelper = new PersistencePathHelper(_storageLocationProvider);
        pathHelper.EnsureDataDirectoryExists();
        var noteId = Guid.NewGuid();
        var json = $$"""
        {
            "Id": "{{noteId}}",
            "Title": "Legacy Note",
            "Tasks": [],
            "Status": "Active",
            "CreatedAt": "2026-01-01T00:00:00Z",
            "UpdatedAt": "2026-01-01T00:00:00Z",
            "DisplayOrder": 0,
            "IsOpened": false,
            "IsPinned": false,
            "IsFavorite": false
        }
        """;
        await File.WriteAllTextAsync(pathHelper.GetNoteFilePath(noteId), json);

        var repository = new FileBasedRepository(_storageLocationProvider);

        // Act
        await repository.InitializeAsync();

        // Assert
        var note = await repository.GetByIdAsync(noteId);
        Assert.IsNotNull(note);
        Assert.AreEqual(NoteType.Todo, note.Type);
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesNoteAndMarksDirty()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
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
        var repository = new FileBasedRepository(_storageLocationProvider);
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
    public async Task GetByStatusAsync_FiltersNotesByStatus()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
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
        var repository = new FileBasedRepository(_storageLocationProvider);
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
        var repository = new FileBasedRepository(_storageLocationProvider);
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
        var repository = new FileBasedRepository(_storageLocationProvider);
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
        var repository = new FileBasedRepository(_storageLocationProvider);
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
        var repository = new FileBasedRepository(_storageLocationProvider);
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
        var repository = new FileBasedRepository(_storageLocationProvider);
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
    public async Task UpdateAsync_PersistsIsPinnedFlag()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "Pinnable" };
        await repository.CreateAsync(note);

        var updatedNote = new StickyNote { Id = note.Id, Title = "Pinnable", IsPinned = true };

        // Act
        await repository.UpdateAsync(updatedNote);

        // Assert
        var retrieved = await repository.GetByIdAsync(note.Id);
        Assert.IsTrue(retrieved!.IsPinned);
    }

    [TestMethod]
    public async Task UpdateAsync_PersistsWindowBounds()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "Positioned" };
        await repository.CreateAsync(note);

        var updatedNote = new StickyNote
        {
            Id = note.Id,
            Title = "Positioned",
            WindowLeft = 100,
            WindowTop = 200,
            WindowWidth = 300,
            WindowHeight = 400
        };

        // Act
        await repository.UpdateAsync(updatedNote);

        // Assert
        var retrieved = await repository.GetByIdAsync(note.Id);
        Assert.AreEqual(100, retrieved!.WindowLeft);
        Assert.AreEqual(200, retrieved.WindowTop);
        Assert.AreEqual(300, retrieved.WindowWidth);
        Assert.AreEqual(400, retrieved.WindowHeight);
    }

    [TestMethod]
    public async Task SaveNoteAsync_PersistsContentFormattingAcrossReload()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();

        var note = new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = "Formatted",
            Content = "Buy milk and bread today",
            ContentFormatting = new RichTextFormatting
            {
                Spans =
                [
                    new RichTextSpan { Start = 4, Length = 4, Bold = true },
                    new RichTextSpan { Start = 13, Length = 5, Italic = true, Underline = true }
                ]
            }
        };
        await repository.CreateAsync(note);

        // Act
        await repository.SaveNoteAsync(note.Id);
        var reloadedRepository = new FileBasedRepository(_storageLocationProvider);
        await reloadedRepository.InitializeAsync();

        // Assert
        var retrieved = await reloadedRepository.GetByIdAsync(note.Id);
        Assert.IsNotNull(retrieved);
        Assert.IsNotNull(retrieved.ContentFormatting);
        Assert.AreEqual(RichTextFormatting.CurrentVersion, retrieved.ContentFormatting.Version);
        Assert.AreEqual(2, retrieved.ContentFormatting.Spans.Count);
        Assert.IsTrue(retrieved.ContentFormatting.Spans.Any(s => s is { Start: 4, Length: 4, Bold: true }));
        Assert.IsTrue(retrieved.ContentFormatting.Spans.Any(s => s is { Start: 13, Length: 5, Italic: true, Underline: true }));
    }

    [TestMethod]
    public async Task SaveNoteAsync_OmitsContentFormattingKeyWhenNull()
    {
        // Arrange - plain/legacy note with no formatting applied
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "Plain", Content = "just text" };
        await repository.CreateAsync(note);

        // Act
        await repository.SaveNoteAsync(note.Id);

        // Assert - the JSON on disk must not contain the key at all, proving old files stay
        // in their original shape and nothing needs to migrate.
        var pathHelper = new PersistencePathHelper(_storageLocationProvider);
        var json = await File.ReadAllTextAsync(pathHelper.GetNoteFilePath(note.Id));
        StringAssert.DoesNotMatch(json, new System.Text.RegularExpressions.Regex("ContentFormatting"));

        var reloadedRepository = new FileBasedRepository(_storageLocationProvider);
        await reloadedRepository.InitializeAsync();
        var retrieved = await reloadedRepository.GetByIdAsync(note.Id);
        Assert.IsNotNull(retrieved);
        Assert.IsNull(retrieved.ContentFormatting);
    }

    [TestMethod]
    public async Task UpdateTask_PersistsTitleFormattingAcrossReload()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "Todo Note" };
        await repository.CreateAsync(note);

        var task = new StickyNoteTask { Id = Guid.NewGuid(), Title = "Buy milk" };
        await ((IStickyNoteTaskRepository)repository).CreateAsync(note.Id, task);

        var updatedTask = new StickyNoteTask
        {
            Id = task.Id,
            Title = "Buy milk",
            TitleFormatting = new RichTextFormatting
            {
                Spans = [new RichTextSpan { Start = 0, Length = 3, Bold = true }]
            }
        };

        // Act
        await ((IStickyNoteTaskRepository)repository).UpdateAsync(note.Id, updatedTask);
        await repository.SaveNoteAsync(note.Id);
        var reloadedRepository = new FileBasedRepository(_storageLocationProvider);
        await reloadedRepository.InitializeAsync();

        // Assert
        var retrieved = await reloadedRepository.GetByIdAsync(note.Id);
        var retrievedTask = retrieved!.Tasks.First(t => t.Id == task.Id);
        Assert.IsNotNull(retrievedTask.TitleFormatting);
        Assert.AreEqual(1, retrievedTask.TitleFormatting.Spans.Count);
        Assert.IsTrue(retrievedTask.TitleFormatting.Spans[0] is { Start: 0, Length: 3, Bold: true });
    }

    [TestMethod]
    public async Task GetByIdAsync_ReturnsNullForNonExistentNote()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.IsNull(result);
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
