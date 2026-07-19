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
    /// Handles dragging the window via the title bar (only when clicking on empty space).
    /// </summary>
    private void OnTitleBarMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Don't drag if clicking on a button, textbox, or other interactive element
        if (e.OriginalSource is System.Windows.Controls.Button ||
            e.OriginalSource is System.Windows.Controls.TextBox ||
            e.OriginalSource is System.Windows.Controls.TextBlock)
        {
            return;
        }

        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            _dragStartPoint = e.GetPosition(this);
            try
            {
                DragMove();
            }
            catch
            {
                // DragMove can throw if called at wrong time
            }

            // Save window position when dragging stops
            if (DataContext is StickyNoteWindowViewModel viewModel)
            {
                viewModel.WindowX = Left;
                viewModel.WindowY = Top;
            }
        }
    }

    /// <summary>
    /// Prevents dragging when clicking in the title textbox.
    /// </summary>
    private void OnTitleTextBoxMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = false;
    }

    /// <summary>
    /// Handles the add task button click to focus the input field.
    /// </summary>
    private void OnAddTaskButtonClick(object sender, RoutedEventArgs e)
    {
        AddTaskInput?.Focus();
        AddTaskInput?.SelectAll();
    }

    /// <summary>
    /// Handles clicking the placeholder text to focus the input field.
    /// </summary>
    private void OnAddTaskPlaceholderClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        AddTaskInput?.Focus();
    }

    /// <summary>
    /// Handles pressing Enter in the add task input field to add a task.
    /// </summary>
    private void OnAddTaskKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Return && DataContext is StickyNoteWindowViewModel viewModel)
        {
            viewModel.AddTaskCommand.Execute(null);
            e.Handled = true;
        }
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
