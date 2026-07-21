using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using StickyDo.Widget.ViewModels;
using StickyDo.Widget.Behaviors;

namespace StickyDo.Widget;

/// <summary>
/// Floating window for displaying and editing a sticky note with task list.
/// Pure MVVM - all interactions through bindings and commands.
/// </summary>
public partial class StickyNoteWindow : Window
{
    public StickyNoteWindow()
    {
        InitializeComponent();

        // Attach behaviors for window-specific operations
        WindowBehavior.AttachToWindow(this);

        // Handle Escape key to close color picker
        this.PreviewKeyDown += StickyNoteWindow_PreviewKeyDown;

        // Handle mouse clicks outside to close color picker
        this.MouseDown += StickyNoteWindow_MouseDown;
    }

    private void StickyNoteWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is StickyNoteWindowViewModel viewModel)
        {
            if (viewModel.IsColorPickerOpen)
            {
                viewModel.CloseColorPickerCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void StickyNoteWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is StickyNoteWindowViewModel viewModel && viewModel.IsColorPickerOpen)
        {
            var hitTest = VisualTreeHelper.HitTest(this, Mouse.GetPosition(this));
            if (hitTest?.VisualHit is not null)
            {
                var isColorPopup = IsDescendantOf(hitTest.VisualHit, ColorPickerPopup.Child);
                var isPaletteButton = IsDescendantOf(hitTest.VisualHit, (Visual)this.FindName("PaletteButton"));

                if (!isColorPopup && !isPaletteButton)
                {
                    viewModel.CloseColorPickerCommand.Execute(null);
                }
            }
        }
    }

    private bool IsDescendantOf(DependencyObject child, DependencyObject? parent)
    {
        if (parent is null) return false;

        var currentParent = child;
        while (currentParent is not null)
        {
            if (currentParent == parent)
                return true;
            currentParent = VisualTreeHelper.GetParent(currentParent);
        }
        return false;
    }
}
