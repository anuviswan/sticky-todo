using System.Windows;
using System.Windows.Controls;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Behavior to handle MainWindow-specific operations in an MVVM-friendly way.
/// </summary>
public static class MainWindowBehavior
{
    /// <summary>
    /// Attaches the behavior to the main window.
    /// </summary>
    public static void AttachToWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        // Wire up title bar drag
        var titleBar = window.FindName("TitleBarBorder") as Border;
        if (titleBar != null)
        {
            titleBar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    try
                    {
                        window.DragMove();
                    }
                    catch
                    {
                        // DragMove can throw if called at wrong time
                    }
                }
            };
        }

        // Wire up window loaded event to load notes
        window.Loaded += (s, e) =>
        {
            if (window.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.LoadNotesCommand.Execute(null);
            }
        };
    }
}
