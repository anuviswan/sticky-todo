using System.Windows;
using System.Windows.Input;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget;

/// <summary>
/// Main window for the Todo List sticky note manager application.
/// </summary>
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

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCreateNoteClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Create note functionality - Phase 2", "Feature Placeholder");
    }

    private void OnDeleteNoteClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Delete note functionality - Phase 2", "Feature Placeholder");
    }

    private void OnSearchTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // TODO: Implement search filtering in Phase 2
    }

    private void OnNoteItemClick(object sender, MouseButtonEventArgs e)
    {
        MessageBox.Show("Open note editor - Phase 2", "Feature Placeholder");
    }
}