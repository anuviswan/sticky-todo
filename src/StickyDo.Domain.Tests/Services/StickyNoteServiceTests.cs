using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Domain.Storage;

namespace StickyDo.Domain.Tests.Services;

[TestClass]
public class StickyNoteServiceTests
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

    private async Task<(StickyNoteService Service, Guid NoteId)> CreateServiceWithNoteAsync()
    {
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        var noteId = await service.CreateNoteAsync("Test Note");
        return (service, noteId);
    }

    [TestMethod]
    public async Task CreateNoteAsync_WithColor_SetsColorArgb()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        var noteId = await service.CreateNoteAsync("Test Note", 0xFFAABBCC);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual((uint)0xFFAABBCC, note!.ColorArgb);
    }

    [TestMethod]
    public async Task CreateNoteAsync_WithoutColor_LeavesColorArgbNull()
    {
        // Arrange & Act
        var (service, noteId) = await CreateServiceWithNoteAsync();

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.IsNull(note!.ColorArgb);
    }

    [TestMethod]
    public async Task SetNotePinnedAsync_PinsNote()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();

        // Act
        await service.SetNotePinnedAsync(noteId, true);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.IsTrue(note!.IsPinned);
    }

    [TestMethod]
    public async Task SetNotePinnedAsync_UnpinsNote()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        await service.SetNotePinnedAsync(noteId, true);

        // Act
        await service.SetNotePinnedAsync(noteId, false);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.IsFalse(note!.IsPinned);
    }

    [TestMethod]
    public async Task SetNotePinnedAsync_DoesNotChangeTitleOrColor()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        await service.UpdateNoteAsync(noteId, "Test Note", StickyNoteStatus.Active, 0xFFAABBCC);

        // Act
        await service.SetNotePinnedAsync(noteId, true);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual("Test Note", note!.Title);
        Assert.AreEqual((uint)0xFFAABBCC, note.ColorArgb);
    }

    [TestMethod]
    public async Task SetNotePinnedAsync_Pinning_DoesNotChangeUpdatedAt()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        var beforeUpdatedAt = (await service.GetNoteByIdAsync(noteId))!.UpdatedAt;

        // Act
        await service.SetNotePinnedAsync(noteId, true);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual(beforeUpdatedAt, note!.UpdatedAt);
    }

    [TestMethod]
    public async Task SetNotePinnedAsync_Unpinning_DoesNotChangeUpdatedAt()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        await service.SetNotePinnedAsync(noteId, true);
        var beforeUpdatedAt = (await service.GetNoteByIdAsync(noteId))!.UpdatedAt;

        // Act
        await service.SetNotePinnedAsync(noteId, false);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual(beforeUpdatedAt, note!.UpdatedAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task SetNotePinnedAsync_ThrowsOnEmptyId()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        await service.SetNotePinnedAsync(Guid.Empty, true);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task SetNotePinnedAsync_ThrowsWhenNoteNotFound()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        await service.SetNotePinnedAsync(Guid.NewGuid(), true);
    }

    [TestMethod]
    public async Task SetNoteFavoriteAsync_MarksNoteAsFavorite()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();

        // Act
        await service.SetNoteFavoriteAsync(noteId, true);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.IsTrue(note!.IsFavorite);
    }

    [TestMethod]
    public async Task SetNoteFavoriteAsync_UnmarksNoteAsFavorite()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        await service.SetNoteFavoriteAsync(noteId, true);

        // Act
        await service.SetNoteFavoriteAsync(noteId, false);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.IsFalse(note!.IsFavorite);
    }

    [TestMethod]
    public async Task SetNoteFavoriteAsync_DoesNotChangeTitleOrColor()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        await service.UpdateNoteAsync(noteId, "Test Note", StickyNoteStatus.Active, 0xFFAABBCC);

        // Act
        await service.SetNoteFavoriteAsync(noteId, true);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual("Test Note", note!.Title);
        Assert.AreEqual((uint)0xFFAABBCC, note.ColorArgb);
    }

    [TestMethod]
    public async Task SetNoteFavoriteAsync_Favoriting_DoesNotChangeUpdatedAt()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        var beforeUpdatedAt = (await service.GetNoteByIdAsync(noteId))!.UpdatedAt;

        // Act
        await service.SetNoteFavoriteAsync(noteId, true);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual(beforeUpdatedAt, note!.UpdatedAt);
    }

    [TestMethod]
    public async Task SetNoteFavoriteAsync_Unfavoriting_DoesNotChangeUpdatedAt()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        await service.SetNoteFavoriteAsync(noteId, true);
        var beforeUpdatedAt = (await service.GetNoteByIdAsync(noteId))!.UpdatedAt;

        // Act
        await service.SetNoteFavoriteAsync(noteId, false);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual(beforeUpdatedAt, note!.UpdatedAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task SetNoteFavoriteAsync_ThrowsOnEmptyId()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        await service.SetNoteFavoriteAsync(Guid.Empty, true);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task SetNoteFavoriteAsync_ThrowsWhenNoteNotFound()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        await service.SetNoteFavoriteAsync(Guid.NewGuid(), true);
    }

    [TestMethod]
    public async Task CreateNoteAsync_Default_SetsTypeToTodo()
    {
        // Arrange & Act
        var (service, noteId) = await CreateServiceWithNoteAsync();

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual(NoteType.Todo, note!.Type);
    }

    [TestMethod]
    public async Task CreateNoteAsync_WithNoteType_PersistsNoteType()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        var noteId = await service.CreateNoteAsync("Test Note", type: NoteType.Note);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual(NoteType.Note, note!.Type);
    }

    [TestMethod]
    public async Task CreateNoteAsync_FirstNoteEver_TodoType_SeedsDemoTask()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        var noteId = await service.CreateNoteAsync("Test Note", type: NoteType.Todo);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual(1, note!.Tasks.Count);
        Assert.AreEqual("First Task", note.Tasks[0].Title);
    }

    [TestMethod]
    public async Task CreateNoteAsync_FirstNoteEver_NoteType_DoesNotSeedDemoTask()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        var noteId = await service.CreateNoteAsync("Test Note", type: NoteType.Note);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual(0, note!.Tasks.Count);
    }

    [TestMethod]
    public async Task CreateNoteAsync_SecondNote_DoesNotSeedDemoTask()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);
        await service.CreateNoteAsync("First Note", type: NoteType.Todo);

        // Act
        var secondNoteId = await service.CreateNoteAsync("Second Note", type: NoteType.Todo);

        // Assert
        var note = await service.GetNoteByIdAsync(secondNoteId);
        Assert.AreEqual(0, note!.Tasks.Count);
    }

    [TestMethod]
    public async Task CreateNoteAsync_EleventhNote_DoesNotSeedDemoTask()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);
        for (var i = 0; i < 10; i++)
            await service.CreateNoteAsync($"Note {i}", type: NoteType.Todo);

        // Act
        var eleventhNoteId = await service.CreateNoteAsync("Note 11", type: NoteType.Todo);

        // Assert
        var note = await service.GetNoteByIdAsync(eleventhNoteId);
        Assert.AreEqual(0, note!.Tasks.Count);
    }

    [TestMethod]
    public async Task SetNoteTypeAsync_ChangesTypeFromTodoToNote()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();

        // Act
        await service.SetNoteTypeAsync(noteId, NoteType.Note);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual(NoteType.Note, note!.Type);
    }

    [TestMethod]
    public async Task SetNoteTypeAsync_ChangesTypeFromNoteToTodo()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        await service.SetNoteTypeAsync(noteId, NoteType.Note);

        // Act
        await service.SetNoteTypeAsync(noteId, NoteType.Todo);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual(NoteType.Todo, note!.Type);
    }

    [TestMethod]
    public async Task SetNoteTypeAsync_DoesNotChangeTitleOrColor()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        await service.UpdateNoteAsync(noteId, "Test Note", StickyNoteStatus.Active, 0xFFAABBCC);

        // Act
        await service.SetNoteTypeAsync(noteId, NoteType.Note);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual("Test Note", note!.Title);
        Assert.AreEqual((uint)0xFFAABBCC, note.ColorArgb);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task SetNoteTypeAsync_ThrowsOnEmptyId()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        await service.SetNoteTypeAsync(Guid.Empty, NoteType.Note);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task SetNoteTypeAsync_ThrowsWhenNoteNotFound()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        await service.SetNoteTypeAsync(Guid.NewGuid(), NoteType.Note);
    }

    [TestMethod]
    public async Task UpdateNoteAsync_WithContent_PersistsContent()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();

        // Act
        await service.UpdateNoteAsync(noteId, "Test Note", StickyNoteStatus.Active, content: "Some free-form text");

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual("Some free-form text", note!.Content);
    }

    [TestMethod]
    public async Task UpdateNoteAsync_WithoutContent_DoesNotClearExistingContent()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        await service.UpdateNoteAsync(noteId, "Test Note", StickyNoteStatus.Active, content: "Some free-form text");

        // Act
        await service.UpdateNoteAsync(noteId, "Test Note", StickyNoteStatus.Active, 0xFFAABBCC);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual("Some free-form text", note!.Content);
    }

    [TestMethod]
    public async Task UpdateNoteAsync_TitleChange_UpdatesUpdatedAt()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        var beforeUpdatedAt = (await service.GetNoteByIdAsync(noteId))!.UpdatedAt;
        await Task.Delay(10);

        // Act
        await service.UpdateNoteAsync(noteId, "Renamed Note", StickyNoteStatus.Active);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.IsTrue(note!.UpdatedAt > beforeUpdatedAt);
    }

    [TestMethod]
    public async Task UpdateNoteAsync_ContentChange_UpdatesUpdatedAt()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();
        var beforeUpdatedAt = (await service.GetNoteByIdAsync(noteId))!.UpdatedAt;
        await Task.Delay(10);

        // Act
        await service.UpdateNoteAsync(noteId, "Test Note", StickyNoteStatus.Active, content: "New content");

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.IsTrue(note!.UpdatedAt > beforeUpdatedAt);
    }

    [TestMethod]
    public async Task UpdateNoteWindowBoundsAsync_PersistsPositionAndSize()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();

        // Act
        await service.UpdateNoteWindowBoundsAsync(noteId, 120, 240, 320, 400);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.AreEqual(120, note!.WindowLeft);
        Assert.AreEqual(240, note.WindowTop);
        Assert.AreEqual(320, note.WindowWidth);
        Assert.AreEqual(400, note.WindowHeight);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task UpdateNoteWindowBoundsAsync_ThrowsOnEmptyId()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        await service.UpdateNoteWindowBoundsAsync(Guid.Empty, 0, 0, 300, 400);
    }

    [TestMethod]
    public async Task DeleteNoteAsync_RemovesNote()
    {
        // Arrange
        var (service, noteId) = await CreateServiceWithNoteAsync();

        // Act
        await service.DeleteNoteAsync(noteId);

        // Assert
        var note = await service.GetNoteByIdAsync(noteId);
        Assert.IsNull(note);
    }

    [TestMethod]
    public async Task DeleteNoteAsync_DoesNotThrowWhenNoteNotFound()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act & Assert (no exception expected - deleting a non-existent note is a no-op)
        await service.DeleteNoteAsync(Guid.NewGuid());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task DeleteNoteAsync_ThrowsOnEmptyId()
    {
        // Arrange
        var repository = new FileBasedRepository(_storageLocationProvider);
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository, repository);

        // Act
        await service.DeleteNoteAsync(Guid.Empty);
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
