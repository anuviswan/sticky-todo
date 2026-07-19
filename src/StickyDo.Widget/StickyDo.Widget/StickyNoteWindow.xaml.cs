using System.Windows;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget;

/// <summary>
/// Floating window for displaying and editing a sticky note with task list.
/// </summary>
public partial class StickyNoteWindow : Window
{
    private Point _dragStartPoint;

    public StickyNoteWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles dragging the window via the title bar.
    /// </summary>
    private void OnTitleBarMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            _dragStartPoint = e.GetPosition(this);
            DragMove();

            // Save window position when dragging stops
            if (DataContext is StickyNoteWindowViewModel viewModel)
            {
                viewModel.WindowX = Left;
                viewModel.WindowY = Top;
            }
        }
    }

    /// <summary>
    /// Handles clicking the add task area to focus for input.
    /// </summary>
    private void OnAddTaskAreaClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // This is a placeholder click handler - in a full implementation,
        // this could focus an input field for adding tasks
    }

    /// <summary>
    /// Handles the close button click with unsaved changes check.
    /// </summary>
    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is StickyNoteWindowViewModel viewModel)
        {
            if (!viewModel.CanCloseWindow())
            {
                return;
            }
        }

        Close();
    }

    /// <summary>
    /// Saves window state and unregisters from WindowManager when closing.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Save final window state
        if (DataContext is StickyNoteWindowViewModel viewModel)
        {
            viewModel.SaveCommand.Execute(null);
        }
    }
}
