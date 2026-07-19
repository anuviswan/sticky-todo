using System.Windows.Controls;

namespace StickyDo.Widget.Controls;

/// <summary>
/// Reusable control for displaying a list of sticky notes with empty state handling.
/// Visibility of notes and empty state is handled via data binding with CollectionToVisibilityConverter.
/// </summary>
public partial class NotesListView : UserControl
{
    public NotesListView()
    {
        InitializeComponent();
    }
}
