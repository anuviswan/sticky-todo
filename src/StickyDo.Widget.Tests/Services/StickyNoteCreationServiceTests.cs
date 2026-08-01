using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Services;

namespace StickyDo.Widget.Tests.Services;

[TestClass]
public class StickyNoteCreationServiceTests
{
    private FileBasedRepository _repository = null!;
    private StickyNoteService _service = null!;
    private FakeStickyNoteWindowService _windowService = null!;
    private StickyNoteCreationService _creationService = null!;

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
        _windowService = new FakeStickyNoteWindowService();
        _creationService = new StickyNoteCreationService(_service, _windowService, new WeakReferenceMessenger());
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
}
