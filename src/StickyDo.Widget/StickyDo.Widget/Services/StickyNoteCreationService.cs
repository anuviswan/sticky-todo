using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;

namespace StickyDo.Widget.Services;

/// <summary>
/// Implementation of sticky note creation service.
/// Orchestrates creating a new note and opening it in a window.
/// </summary>
public class StickyNoteCreationService : IStickyNoteCreationService
{
    private readonly StickyNoteService _stickyNoteService;
    private readonly IStickyNoteWindowService _windowService;

    public StickyNoteCreationService(
        StickyNoteService stickyNoteService,
        IStickyNoteWindowService windowService)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(windowService);
        _stickyNoteService = stickyNoteService;
        _windowService = windowService;
    }

    public async Task CreateNewNoteAsync()
    {
        try
        {
            var noteNumber = await _stickyNoteService.GetNextNoteNumberAsync();
            var noteTitle = $"Note {noteNumber}";
            var noteId = await _stickyNoteService.CreateNoteAsync(noteTitle);
            await _windowService.OpenNoteWindowAsync(noteId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create new note.", ex);
        }
    }
}
