namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Service interface for managing sticky note window operations.
/// Separates UI concerns from ViewModel logic, maintaining pure MVVM.
/// </summary>
public interface IStickyNoteWindowService
{
    /// <summary>
    /// Opens or focuses a sticky note window for the given note ID.
    /// </summary>
    Task OpenNoteWindowAsync(Guid noteId);
}
