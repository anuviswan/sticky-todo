namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Manages the application's system tray icon, allowing the main window
/// to be reopened after it has been hidden (e.g. because sticky notes were
/// restored as floating windows on startup).
/// </summary>
public interface ITrayIconService : IDisposable
{
    /// <summary>
    /// Creates and shows the tray icon.
    /// </summary>
    /// <param name="onOpenRequested">Invoked when the user asks to reopen the main window (double-click or context menu).</param>
    /// <param name="onExitRequested">Invoked when the user chooses to exit the application from the context menu.</param>
    void Initialize(Action onOpenRequested, Action onExitRequested);
}
