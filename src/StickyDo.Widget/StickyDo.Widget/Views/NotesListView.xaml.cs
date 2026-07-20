using System.Windows.Controls;

namespace StickyDo.Widget.Views;

/// <summary>
/// View for displaying a list of sticky notes with dedicated ViewModel.
/// DataContext is bound via XAML binding to parent MainWindowViewModel.NotesListViewModel.
/// </summary>
public partial class NotesListView : UserControl
{
    public NotesListView()
    {
        InitializeComponent();
    }
}
