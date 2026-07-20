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
}
