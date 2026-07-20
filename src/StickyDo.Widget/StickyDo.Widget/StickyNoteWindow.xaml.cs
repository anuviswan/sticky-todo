using System.Windows;
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
    }
}
