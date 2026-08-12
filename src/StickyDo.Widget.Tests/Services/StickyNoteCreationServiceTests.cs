using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Domain.Storage;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Services;

namespace StickyDo.Widget.Tests.Services;

[TestClass]
public class StickyNoteCreationServiceTests
{
    private string _testDataDirectory = null!;
    private FileBasedRepository _repository = null!;
    private StickyNoteService _service = null!;
    private FakeStickyNoteWindowService _windowService = null!;
    private StickyNoteCreationService _creationService = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "StickyDo_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);

        _repository = new FileBasedRepository(new FakeStorageLocationProvider(_testDataDirectory));
        await _repository.InitializeAsync();
        _service = new StickyNoteService(_repository, _repository);
        _windowService = new FakeStickyNoteWindowService();
        _creationService = new StickyNoteCreationService(_service, _windowService, new WeakReferenceMessenger());
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDataDirectory))
            Directory.Delete(_testDataDirectory, recursive: true);
    }

    [TestMethod]
    public async Task CreateNewNoteAsync_TypeNote_PersistsNoteTypeItem()
    {
        await _creationService.CreateNewNoteAsync(type: NoteType.Note);

        var created = await _service.GetNoteByIdAsync(_windowService.LastOpenedNoteId!.Value);
        Assert.AreEqual(NoteType.Note, created!.Type);
    }

    [TestMethod]
    public async Task CreateNewNoteAsync_TypeOmitted_DefaultsToTodoType()
    {
        await _creationService.CreateNewNoteAsync();

        var created = await _service.GetNoteByIdAsync(_windowService.LastOpenedNoteId!.Value);
        Assert.AreEqual(NoteType.Todo, created!.Type);
    }

    [TestMethod]
    public async Task CreateNewNoteAsync_TypeNote_DoesNotCopyContentFromAnyExistingNote()
    {
        var sourceId = await _service.CreateNoteAsync("Existing Note", type: NoteType.Note);
        await _service.UpdateNoteAsync(sourceId, "Existing Note", StickyNoteStatus.Active, content: "Some existing body text");

        await _creationService.CreateNewNoteAsync(type: NoteType.Note);

        var created = await _service.GetNoteByIdAsync(_windowService.LastOpenedNoteId!.Value);
        Assert.AreNotEqual(sourceId, created!.Id);
        Assert.IsTrue(string.IsNullOrEmpty(created.Content));
    }

    private sealed class FakeStickyNoteWindowService : IStickyNoteWindowService
    {
        public Guid? LastOpenedNoteId { get; private set; }

        public Task OpenNoteWindowAsync(Guid noteId)
        {
            LastOpenedNoteId = noteId;
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
