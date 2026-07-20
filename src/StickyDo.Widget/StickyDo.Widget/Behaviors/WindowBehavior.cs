using System.Windows;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Behavior to handle sticky note window lifecycle.
/// Saves state when the window is closed.
/// </summary>
public static class WindowBehavior
{
    /// <summary>
    /// Attaches the behavior to a window and sets up necessary event handlers.
    /// </summary>
    public static void AttachToWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

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
