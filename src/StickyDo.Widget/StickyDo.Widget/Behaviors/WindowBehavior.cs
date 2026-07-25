using System.Windows;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Behavior to handle sticky note window lifecycle.
/// Saves state when the window moves, resizes, or is closed.
/// </summary>
public static class WindowBehavior
{
    /// <summary>
    /// Attaches the behavior to a window and sets up necessary event handlers.
    /// </summary>
    public static void AttachToWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        // Persist the window's position/size as it's moved or resized, so it survives even if
        // the application is later terminated without a clean shutdown (e.g. force-killed).
        window.LocationChanged += (s, e) =>
        {
            if (window.DataContext is StickyNoteWindowViewModel viewModel)
            {
                _ = viewModel.UpdateWindowBoundsAsync(window.Left, window.Top, window.Width, window.Height);
            }
        };

        window.SizeChanged += (s, e) =>
        {
            if (window.DataContext is StickyNoteWindowViewModel viewModel)
            {
                _ = viewModel.UpdateWindowBoundsAsync(window.Left, window.Top, window.Width, window.Height);
            }
        };

        // Wire up window closed to save state
        window.Closed += async (s, e) =>
        {
            if (window.DataContext is StickyNoteWindowViewModel viewModel)
            {
                await viewModel.SaveAsync();
            }
        };
    }
}
