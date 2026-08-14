using CommunityToolkit.Mvvm.Messaging;
using StickyDo.Domain.Models;
using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Messages;

namespace StickyDo.Widget.Services;

/// <summary>
/// Implementation of sticky note creation service.
/// Orchestrates creating a new note and opening it in a window.
/// </summary>
public class StickyNoteCreationService : IStickyNoteCreationService
{
    private readonly StickyNoteService _stickyNoteService;
    private readonly IStickyNoteWindowService _windowService;
    private readonly IMessenger _messenger;

    public StickyNoteCreationService(
        StickyNoteService stickyNoteService,
        IStickyNoteWindowService windowService,
        IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(messenger);
        _stickyNoteService = stickyNoteService;
        _windowService = windowService;
        _messenger = messenger;
    }

    public async Task CreateNewNoteAsync(uint? colorArgb = null, NoteType type = NoteType.Todo)
    {
        try
        {
            var noteNumber = await _stickyNoteService.GetNextNoteNumberAsync();
            var noteTitle = $"Note {noteNumber}";
            var noteId = await _stickyNoteService.CreateNoteAsync(noteTitle, colorArgb, type);

            // Open the window before notifying the list, so its card reflects the note as
            // loaded (e.g. the demo task seeded into a user's very first note) instead of
            // caching a stale snapshot that nothing ever refreshes afterward.
            await _windowService.OpenNoteWindowAsync(noteId);

            _messenger.Send(new StickyNoteChangedMessage(noteId, StickyNoteChangeType.Created));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create new note.", ex);
        }
    }
}
