using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;

namespace StickyDo.Domain.Tests.Services;

[TestClass]
public class StickyNoteServiceTests
{
    private string _testDataDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "StickyTODO_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var originalPath = Path.Combine(appDataPath, "StickyTODO");

        if (Directory.Exists(originalPath))
        {
            var backupPath = originalPath + ".backup";
            if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, recursive: true);
            Directory.Move(originalPath, backupPath);
        }

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

    private static async Task<(StickyNoteService Service, Guid NoteId)> CreateServiceWithNoteAsync()
    {
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository);

        var noteId = await service.CreateNoteAsync("Test Note");
        return (service, noteId);
    }

    [TestMethod]
    public async Task CreateNoteAsync_WithColor_SetsColorArgb()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository);

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
    [ExpectedException(typeof(ArgumentException))]
    public async Task SetNotePinnedAsync_ThrowsOnEmptyId()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository);

        // Act
        await service.SetNotePinnedAsync(Guid.Empty, true);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task SetNotePinnedAsync_ThrowsWhenNoteNotFound()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository);

        // Act
        await service.SetNotePinnedAsync(Guid.NewGuid(), true);
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
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository);

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
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository);

        // Act & Assert (no exception expected - deleting a non-existent note is a no-op)
        await service.DeleteNoteAsync(Guid.NewGuid());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task DeleteNoteAsync_ThrowsOnEmptyId()
    {
        // Arrange
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var service = new StickyNoteService(repository);

        // Act
        await service.DeleteNoteAsync(Guid.Empty);
    }
}
