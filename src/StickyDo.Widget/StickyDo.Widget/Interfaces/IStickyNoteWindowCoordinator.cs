namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Coordinator for communication between main window and sticky note windows.
/// Handles cross-window operations that require main window involvement.
/// </summary>
public interface IStickyNoteWindowCoordinator
{
    /// <summary>
    /// Creates a new sticky note from within a sticky note window.
    /// Delegates to main window to perform the creation.
    /// </summary>
    Task CreateNewNoteAsync();
}
