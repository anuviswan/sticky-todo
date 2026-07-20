using System.Windows;

namespace StickyDo.Widget.Services;

/// <summary>
/// Stores the position and size state of a window for persistence.
/// </summary>
public record WindowState(double Left, double Top, double Width, double Height);

/// <summary>
/// Service for managing window behavior and state across the application.
/// Handles both the main window and floating sticky note windows.
/// </summary>
public class WindowManager
{
    private Window? _window;
    private readonly Dictionary<Guid, Window> _openNoteWindows = new();
    private readonly Dictionary<Guid, WindowState> _windowStates = new();
    private const string WindowStateKey = "WindowState";
    private const string WindowSizeKey = "WindowSize";
    private const string WindowPositionKey = "WindowPosition";

    public WindowManager()
    {
    }

    /// <summary>
    /// Sets the main window reference after construction (for DI pattern).
    /// </summary>
    public void SetMainWindow(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    /// <summary>
    /// Saves the current window state (position, size, window state).
    /// </summary>
    public void SaveWindowState()
    {
        try
        {
            // TODO: Implement window state persistence to app settings in Phase 2
            // For now, this is a placeholder for the feature
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving window state: {ex.Message}");
        }
    }

    /// <summary>
    /// Restores the window state from saved settings.
    /// </summary>
    public void RestoreWindowState()
    {
        try
        {
            // TODO: Implement window state restoration from app settings in Phase 2
            // For now, use default state
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error restoring window state: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures the window is visible within the screen bounds.
    /// </summary>
    public void EnsureWindowVisible()
    {
        if (_window == null)
            return;

        // TODO: Implement screen bounds checking in Phase 2
        // For now, just ensure basic positioning
        if (_window.Left < 0)
            _window.Left = 0;
        if (_window.Top < 0)
            _window.Top = 0;
    }

    /// <summary>
    /// Registers a floating sticky note window for tracking.
    /// </summary>
    public void RegisterNoteWindow(Guid noteId, Window window)
    {
        if (window == null)
            throw new ArgumentNullException(nameof(window));

        _openNoteWindows[noteId] = window;
    }

    /// <summary>
    /// Unregisters a floating sticky note window.
    /// </summary>
    public void UnregisterNoteWindow(Guid noteId)
    {
        _openNoteWindows.Remove(noteId);
    }

    /// <summary>
    /// Checks if a sticky note window is already open.
    /// </summary>
    public bool IsNoteWindowOpen(Guid noteId)
    {
        return _openNoteWindows.ContainsKey(noteId);
    }

    /// <summary>
    /// Gets an open sticky note window by note ID.
    /// </summary>
    public Window? GetNoteWindow(Guid noteId)
    {
        _openNoteWindows.TryGetValue(noteId, out var window);
        return window;
    }

    /// <summary>
    /// Gets all currently open sticky note windows.
    /// </summary>
    public IEnumerable<Window> GetAllNoteWindows()
    {
        return _openNoteWindows.Values;
    }

    /// <summary>
    /// Closes all open sticky note windows.
    /// </summary>
    public void CloseAllNoteWindows()
    {
        var windowsToClose = _openNoteWindows.Values.ToList();
        foreach (var window in windowsToClose)
        {
            try
            {
                window.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing note window: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Saves window position and size for a sticky note.
    /// </summary>
    public void SaveNoteWindowState(Guid noteId, double left, double top, double width, double height)
    {
        _windowStates[noteId] = new WindowState(left, top, width, height);
    }

    /// <summary>
    /// Restores previously saved window position and size for a sticky note.
    /// </summary>
    public WindowState? GetSavedNoteWindowState(Guid noteId)
    {
        _windowStates.TryGetValue(noteId, out var state);
        return state;
    }
}
