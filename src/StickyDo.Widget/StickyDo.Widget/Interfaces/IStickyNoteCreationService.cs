namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Service for creating new sticky notes and opening them in windows.
/// Orchestrates the complete workflow: create note + display in window.
/// </summary>
public interface IStickyNoteCreationService
{
    /// <summary>
    /// Creates a new sticky note and opens it in a window, optionally inheriting a color
    /// (e.g. from the note it was created from).
    /// </summary>
    Task CreateNewNoteAsync(uint? colorArgb = null);
}
