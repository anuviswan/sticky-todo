using StickyDo.Domain.Services;

namespace StickyDo.Widget.Services;

/// <summary>
/// Implementation of sticky note window coordinator.
/// Delegates note creation to the main window's ViewModel.
/// </summary>
public class StickyNoteWindowCoordinator : IStickyNoteWindowCoordinator
{
    private readonly StickyNoteService _stickyNoteService;
    private readonly IStickyNoteWindowService _windowService;

    public StickyNoteWindowCoordinator(
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
