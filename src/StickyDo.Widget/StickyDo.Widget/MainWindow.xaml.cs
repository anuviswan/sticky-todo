using System.Windows;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void SetViewModel(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
    }

    private void OnAllNotesNavClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowAllNotes();
        }
    }

    private void OnFavoritesNavClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowFavorites();
        }
    }

    private void OnTrashNavClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowTrash();
        }
    }

    private void OnCreateNoteClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            _ = vm.CreateNoteAsync();
        }
    }

    private void OnSearchTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SearchQuery = SearchTextBox.Text;
            _ = vm.SearchNotesAsync();
        }
    }
}
