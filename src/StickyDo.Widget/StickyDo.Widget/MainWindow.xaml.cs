using System.Windows;
using StickyDo.Widget.ViewModels;
using StickyDo.Widget.Behaviors;

namespace StickyDo.Widget;

/// <summary>
/// Main application window for the Sticky TODO application.
/// Pure MVVM - all logic in ViewModels and Behaviors.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        // Set DataContext from injected ViewModel
        DataContext = viewModel;

        // Attach behaviors for window-specific operations
        MainWindowBehavior.AttachToWindow(this);
    }
}
