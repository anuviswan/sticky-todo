namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Abstraction for window operations (minimize, close, etc).
/// Keeps ViewModels view-agnostic.
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Requests that the main window be minimized.
    /// </summary>
    void RequestMinimize();

    /// <summary>
    /// Requests that the main window be closed.
    /// </summary>
    void RequestClose();

    /// <summary>
    /// Shows the main window, restoring it from a minimized or hidden (tray-only) state
    /// if necessary, and brings it to the foreground. If already open, this simply focuses it.
    /// </summary>
    void RequestShow();
}
