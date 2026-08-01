using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Domain.Storage;

namespace StickyDo.Domain.Tests.Services;

[TestClass]
public class PersistenceServiceTests
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
    public async Task SaveAllDirtyNotesAsync_RaisesSavingThenSavedForDirtyNote()
    {
        var repository = new FileBasedRepository(_storageLocationProvider);
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
        var repository = new FileBasedRepository(_storageLocationProvider);
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
        var repository = new FileBasedRepository(_storageLocationProvider);
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
        var repository = new FileBasedRepository(_storageLocationProvider);
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
