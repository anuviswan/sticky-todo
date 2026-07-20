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
    public MainWindow()
    {
        InitializeComponent();

        // Attach behaviors for window-specific operations
        MainWindowBehavior.AttachToWindow(this);
    }

    /// <summary>
    /// Sets the view model for this window.
    /// Child views bind to their ViewModels via XAML DataContext bindings.
    /// </summary>
    public void SetViewModel(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
    }
}
