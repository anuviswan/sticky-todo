using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;

namespace StickyDo.Domain.Tests.Services;

[TestClass]
public class PersistenceServiceTests
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

    [TestMethod]
    public async Task SaveAllDirtyNotesAsync_RaisesSavingThenSavedForDirtyNote()
    {
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var persistenceService = new PersistenceService(repository);

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "Test Note" };
        await repository.CreateAsync(note);

        var states = new List<(Guid NoteId, NoteSaveState State)>();
        persistenceService.NoteSaveStateChanged += (_, e) => states.Add((e.NoteId, e.State));

        await persistenceService.SaveAllDirtyNotesAsync();

        CollectionAssert.AreEqual(
            new[] { NoteSaveState.Saving, NoteSaveState.Saved },
            states.Where(s => s.NoteId == note.Id).Select(s => s.State).ToArray());
    }

    [TestMethod]
    public async Task SaveAllDirtyNotesAsync_ClearsDirtyStateAfterSaving()
    {
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var persistenceService = new PersistenceService(repository);

        var note = new StickyNote { Id = Guid.NewGuid(), Title = "Test Note" };
        await repository.CreateAsync(note);

        Assert.IsTrue(persistenceService.HasPendingChanges);

        await persistenceService.SaveAllDirtyNotesAsync();

        Assert.IsFalse(persistenceService.HasPendingChanges);
    }

    [TestMethod]
    public async Task SaveAllDirtyNotesAsync_WithNoDirtyNotes_RaisesNoEvents()
    {
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var persistenceService = new PersistenceService(repository);

        var eventRaised = false;
        persistenceService.NoteSaveStateChanged += (_, _) => eventRaised = true;

        await persistenceService.SaveAllDirtyNotesAsync();

        Assert.IsFalse(eventRaised);
    }

    [TestMethod]
    public async Task SaveAllDirtyNotesAsync_SavesEachDirtyNoteIndependently()
    {
        var repository = new FileBasedRepository();
        await repository.InitializeAsync();
        var persistenceService = new PersistenceService(repository);

        var note1 = new StickyNote { Id = Guid.NewGuid(), Title = "Note 1" };
        var note2 = new StickyNote { Id = Guid.NewGuid(), Title = "Note 2" };
        await repository.CreateAsync(note1);
        await repository.CreateAsync(note2);

        var savedNoteIds = new List<Guid>();
        persistenceService.NoteSaveStateChanged += (_, e) =>
        {
            if (e.State == NoteSaveState.Saved)
                savedNoteIds.Add(e.NoteId);
        };

        await persistenceService.SaveAllDirtyNotesAsync();

        CollectionAssert.AreEquivalent(new[] { note1.Id, note2.Id }, savedNoteIds);
    }
}
