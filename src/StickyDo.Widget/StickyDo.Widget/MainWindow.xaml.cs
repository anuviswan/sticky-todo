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
}
