using System.Windows;
using System.Windows.Controls;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Behavior to handle window-specific operations in an MVVM-friendly way.
/// Eliminates the need for code-behind event handlers.
/// </summary>
public static class WindowBehavior
{
    /// <summary>
    /// Attaches the behavior to a window and sets up all necessary event handlers.
    /// </summary>
    public static void AttachToWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        // Wire up DataContext changed to set up callbacks
        window.DataContextChanged += (s, e) =>
        {
            if (window.DataContext is StickyNoteWindowViewModel viewModel)
            {
                SetupViewModelCallbacks(window, viewModel);
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

        // Wire up title bar drag - check source to avoid dragging when clicking buttons
        var titleBar = window.FindName("TitleBarBorder") as Border;
        if (titleBar != null)
        {
            titleBar.MouseLeftButtonDown += (s, e) =>
            {
                // Check if click is on an interactive element
                if (e.OriginalSource is System.Windows.Controls.Button ||
                    e.OriginalSource is System.Windows.Controls.TextBox)
                {
                    return;
                }

                if (window.DataContext is StickyNoteWindowViewModel viewModel)
                {
                    viewModel.OnTitleBarMouseDown();
                }
            };
        }
    }

    /// <summary>
    /// Sets up callbacks between Window and ViewModel for operations that require Window class access.
    /// </summary>
    private static void SetupViewModelCallbacks(Window window, StickyNoteWindowViewModel viewModel)
    {
        // Callback for drag operations
        viewModel.SetDragWindowCallback(() =>
        {
            try
            {
                window.DragMove();
                viewModel.WindowX = window.Left;
                viewModel.WindowY = window.Top;
            }
            catch
            {
                // DragMove can throw if called at wrong time
            }
        });

        // Callback for focusing the input field
        var addTaskInput = window.FindName("AddTaskInput") as System.Windows.Controls.TextBox;
        viewModel.SetFocusAddTaskInputCallback(() =>
        {
            addTaskInput?.Focus();
            addTaskInput?.SelectAll();
        });

        // Callback for closing the window
        viewModel.SetCloseWindowCallback((shouldClose) =>
        {
            if (shouldClose)
            {
                window.Close();
            }
        });
    }
}
