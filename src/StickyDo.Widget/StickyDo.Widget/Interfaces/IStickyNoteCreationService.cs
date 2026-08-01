using StickyDo.Domain.Models;

namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Service for creating new sticky notes and opening them in windows.
/// Orchestrates the complete workflow: create note + display in window.
/// </summary>
public interface IStickyNoteCreationService
{
    /// <summary>
    /// Creates a new sticky note and opens it in a window, optionally inheriting a color
    /// (e.g. from the note it was created from). The note type defaults to <see cref="NoteType.Todo"/>
    /// but callers creating from an existing note (e.g. the floating window's "+" button) should pass
    /// that note's type so the new note matches it.
    /// </summary>
    Task CreateNewNoteAsync(uint? colorArgb = null, NoteType type = NoteType.Todo);
}
