using System.Windows;
using StickyDo.Domain.Services;
using StickyDo.Widget.ViewModels;
using StickyDo.Widget.Behaviors;
using StickyDo.Widget.Interfaces;

namespace StickyDo.Widget;

/// <summary>
/// Main application window for the Sticky TODO application.
/// Pure MVVM - all logic in ViewModels and Behaviors.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(StickyNoteService stickyNoteService, IStickyNoteWindowService noteWindowService,
        IDialogService dialogService, IWindowService windowService)
    {
        InitializeComponent();

        // Create view models with injected services
        var notesListViewModel = new NotesListViewModel(stickyNoteService, noteWindowService, dialogService);
        var mainWindowViewModel = new MainWindowViewModel(windowService, notesListViewModel);

        // Set DataContext
        DataContext = mainWindowViewModel;

        // Attach behaviors for window-specific operations
        MainWindowBehavior.AttachToWindow(this);
    }
}
