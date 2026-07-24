using CommunityToolkit.Mvvm.Messaging;
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

    public async Task CreateNewNoteAsync()
    {
        try
        {
            var noteNumber = await _stickyNoteService.GetNextNoteNumberAsync();
            var noteTitle = $"Note {noteNumber}";
            var noteId = await _stickyNoteService.CreateNoteAsync(noteTitle);
            _messenger.Send(new StickyNoteChangedMessage(noteId, StickyNoteChangeType.Created));
            await _windowService.OpenNoteWindowAsync(noteId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create new note.", ex);
        }
    }
}
