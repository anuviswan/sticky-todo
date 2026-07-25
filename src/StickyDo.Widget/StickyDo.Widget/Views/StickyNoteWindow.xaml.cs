using System.Windows;

namespace StickyDo.Widget.Views;

/// <summary>
/// Floating window for displaying and editing a sticky note with task list.
/// Pure MVVM - all interactions through bindings, commands and attached behaviors.
/// </summary>
public partial class StickyNoteWindow : Window
{
    public StickyNoteWindow()
    {
        InitializeComponent();
    }
}
