using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickyDo.Widget.Resources;
using StickyDo.Widget.Services;
using StickyDo.Widget.Utilities;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// Represents the currently selected navigation view.
/// </summary>
public enum NavigationView
{
    AllNotes,
    Favorites,
    Trash
}

/// <summary>
/// ViewModel for the main application window managing navigation and window operations.
/// Pure MVVM - delegates notes list management to NotesListViewModel.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IWindowService _mainWindowService;

    [ObservableProperty]
    private NotesListViewModel notesListViewModel;

    [ObservableProperty]
    private NavigationView selectedNavView = NavigationView.AllNotes;

    [ObservableProperty]
    private string syncStatus = AppStrings.SyncedStatus;

    [ObservableProperty]
    private string noteCountDisplay = AppStrings.ZeroNotes;

    [ObservableProperty]
    private string lastSyncDisplay = AppStrings.JustNow;

    public MainWindowViewModel(
        IWindowService mainWindowService,
        NotesListViewModel notesListViewModel)
    {
        ArgumentNullException.ThrowIfNull(mainWindowService);
        ArgumentNullException.ThrowIfNull(notesListViewModel);
        _mainWindowService = mainWindowService;
        NotesListViewModel = notesListViewModel;
    }

    /// <summary>
    /// Loads all sticky notes by delegating to NotesListViewModel.
    /// </summary>
    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        try
        {
            SyncStatus = AppStrings.SyncingStatus;
            await NotesListViewModel.LoadNotesAsync();
            SyncStatus = AppStrings.SyncedStatus;
            LastSyncDisplay = AppStrings.JustNow;
        }
        catch (Exception ex)
        {
            SyncStatus = AppStrings.ErrorStatus;
            LoggerHelper.LogException(ex, nameof(LoadNotesAsync));
        }
    }

    /// <summary>
    /// Shows all notes by clearing search filter (placeholder for Phase 2).
    /// </summary>
    [RelayCommand]
    public void ShowAllNotes()
    {
        SelectedNavView = NavigationView.AllNotes;
        NotesListViewModel.SearchQuery = string.Empty;
    }

    /// <summary>
    /// Shows favorite notes (placeholder for Phase 2).
    /// </summary>
    [RelayCommand]
    public void ShowFavorites()
    {
        SelectedNavView = NavigationView.Favorites;
        NotesListViewModel.SearchQuery = string.Empty;
    }

    /// <summary>
    /// Shows trash notes (placeholder for Phase 2).
    /// </summary>
    [RelayCommand]
    public void ShowTrash()
    {
        SelectedNavView = NavigationView.Trash;
        NotesListViewModel.SearchQuery = string.Empty;
    }

    /// <summary>
    /// Minimizes the application window.
    /// </summary>
    [RelayCommand]
    public void MinimizeWindow()
    {
        _mainWindowService.RequestMinimize();
    }

    /// <summary>
    /// Closes the application window.
    /// </summary>
    [RelayCommand]
    public void CloseWindow()
    {
        _mainWindowService.RequestClose();
    }

}
