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
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.FilteredNotes))
            {
                UpdateEmptyState();
            }
        };
    }

    private void UpdateEmptyState()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var hasNotes = vm.FilteredNotes?.Count > 0;
            NotesScrollViewer.Visibility = hasNotes == true ? Visibility.Visible : Visibility.Collapsed;
            EmptyStatePanel.Visibility = hasNotes == true ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void OnAllNotesNavClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowAllNotes();
            UpdateNavButtonStyles("all");
        }
    }

    private void OnFavoritesNavClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowFavorites();
            UpdateNavButtonStyles("favorites");
        }
    }

    private void OnTrashNavClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowTrash();
            UpdateNavButtonStyles("trash");
        }
    }

    private void UpdateNavButtonStyles(string active)
    {
        AllNotesNavButton.Opacity = active == "all" ? 1.0 : 0.6;
        FavoritesNavButton.Opacity = active == "favorites" ? 1.0 : 0.6;
        TrashNavButton.Opacity = active == "trash" ? 1.0 : 0.6;
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

    private void OnNoteItemClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Placeholder for note item interaction
    }
}
