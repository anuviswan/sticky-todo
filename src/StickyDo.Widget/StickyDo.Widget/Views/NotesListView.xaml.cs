using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StickyDo.Widget.ViewModels;

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

    /// <summary>
    /// Opens a note as a floating sticky note window when double-clicked in the list.
    /// </summary>
    private void StickyNoteListItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2)
            return;

        if (sender is FrameworkElement { DataContext: StickyNoteItemViewModel note } &&
            DataContext is NotesListViewModel viewModel)
        {
            _ = viewModel.OpenNoteAsync(note.Id);
        }
    }
}
