using System.Windows;
using System.Windows.Input;
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

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }
}
