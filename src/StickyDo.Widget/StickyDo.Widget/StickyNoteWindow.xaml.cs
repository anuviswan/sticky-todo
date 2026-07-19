using System.Windows;
using System.Windows.Input;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget;

/// <summary>
/// Floating window for displaying and editing a sticky note with task list.
/// </summary>
public partial class StickyNoteWindow : Window
{
    public StickyNoteWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// When data context changes, set up callbacks with the view model.
    /// </summary>
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        if (DataContext is StickyNoteWindowViewModel viewModel)
        {
            // Set callbacks for UI operations that can only be performed from the Window class
            viewModel.SetDragWindowCallback(() =>
            {
                try
                {
                    DragMove();
                    viewModel.WindowX = Left;
                    viewModel.WindowY = Top;
                }
                catch
                {
                    // DragMove can throw if called at wrong time
                }
            });

            viewModel.SetFocusAddTaskInputCallback(() =>
            {
                AddTaskInput?.Focus();
                AddTaskInput?.SelectAll();
            });

            viewModel.SetCloseWindowCallback((shouldClose) =>
            {
                if (shouldClose)
                {
                    Close();
                }
            });
        }
    }

    /// <summary>
    /// Prevent dragging when clicking on interactive elements.
    /// </summary>
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDown(e);

        // Don't trigger drag if clicking on buttons, textboxes, or other interactive elements
        if (e.OriginalSource is System.Windows.Controls.Button ||
            e.OriginalSource is System.Windows.Controls.TextBox)
        {
            e.Handled = true;
            return;
        }

        // Allow drag to proceed through the InputBinding
    }

    /// <summary>
    /// Saves window state when window is closed.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (DataContext is StickyNoteWindowViewModel viewModel)
        {
            viewModel.SaveCommand.Execute(null);
        }
    }
}
