using System.Windows;

namespace StickyDo.Widget.Services;

/// <summary>
/// Service for managing window behavior and state.
/// </summary>
public class WindowManager
{
    private readonly Window _window;
    private const string WindowStateKey = "WindowState";
    private const string WindowSizeKey = "WindowSize";
    private const string WindowPositionKey = "WindowPosition";

    public WindowManager(Window window)
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
        // TODO: Implement screen bounds checking in Phase 2
        // For now, just ensure basic positioning
        if (_window.Left < 0)
            _window.Left = 0;
        if (_window.Top < 0)
            _window.Top = 0;
    }
}
