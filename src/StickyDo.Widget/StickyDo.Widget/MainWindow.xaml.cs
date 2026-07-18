using System.Windows;
using System.Windows.Input;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget;

public partial class MainWindow : Window
{
    private const string PlaceholderText = "Search tasks, notes, or labels...";

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnMainWindowLoaded;
    }

    public void SetViewModel(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (SearchTextBox != null)
        {
            SearchTextBox.GotFocus += OnSearchTextBoxGotFocus;
            SearchTextBox.LostFocus += OnSearchTextBoxLostFocus;
            if (string.IsNullOrEmpty(SearchTextBox.Text))
                SetPlaceholder();
        }
    }

    private void OnSearchTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (SearchTextBox.Text == PlaceholderText)
            SearchTextBox.Text = string.Empty;
    }

    private void OnSearchTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SearchTextBox.Text))
            SetPlaceholder();
    }

    private void SetPlaceholder()
    {
        SearchTextBox.Text = PlaceholderText;
        SearchTextBox.Foreground = System.Windows.Media.Brushes.LightGray;
    }
}
