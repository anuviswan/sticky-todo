using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickyDo.Domain.Models;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Services;
using StickyDo.Widget.Utilities;
using AppResources = StickyDo.Widget.Resources.Resources;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// Represents the currently selected navigation view.
/// </summary>
public enum NavigationView
{
    AllNotes,
    Todos,
    Notes,
    Favorites
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
    private SettingsViewModel settings;

    [ObservableProperty]
    private bool isSettingsOpen;

    [ObservableProperty]
    private NavigationView selectedNavView = NavigationView.AllNotes;

    [ObservableProperty]
    private string syncStatus = AppResources.SyncedStatus;

    [ObservableProperty]
    private string noteCountDisplay = AppResources.ZeroNotes;

    [ObservableProperty]
    private string lastSyncDisplay = AppResources.JustNow;

    public MainWindowViewModel(
        IWindowService mainWindowService,
        NotesListViewModel notesListViewModel)
    {
        ArgumentNullException.ThrowIfNull(mainWindowService);
        ArgumentNullException.ThrowIfNull(notesListViewModel);
        _mainWindowService = mainWindowService;
        NotesListViewModel = notesListViewModel;

        Settings = new SettingsViewModel();
        Settings.CloseRequested += (s, e) => IsSettingsOpen = false;
    }

    /// <summary>
    /// Loads all sticky notes by delegating to NotesListViewModel.
    /// </summary>
    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        try
        {
            SyncStatus = AppResources.SyncingStatus;
            await NotesListViewModel.LoadNotesAsync();
            SyncStatus = AppResources.SyncedStatus;
            LastSyncDisplay = AppResources.JustNow;
        }
        catch (Exception ex)
        {
            SyncStatus = AppResources.ErrorStatus;
            LoggerHelper.LogException(ex, nameof(LoadNotesAsync));
        }
    }

    /// <summary>
    /// Shows all notes by clearing the search filter and the Favorites-only filter.
    /// </summary>
    [RelayCommand]
    public void ShowAllNotes()
    {
        IsSettingsOpen = false;
        SelectedNavView = NavigationView.AllNotes;
        NotesListViewModel.SearchQuery = string.Empty;
        NotesListViewModel.ShowFavoritesOnly = false;
        NotesListViewModel.TypeFilter = null;
    }

    /// <summary>
    /// Shows only notes of type Todo.
    /// </summary>
    [RelayCommand]
    public void ShowTodos()
    {
        IsSettingsOpen = false;
        SelectedNavView = NavigationView.Todos;
        NotesListViewModel.SearchQuery = string.Empty;
        NotesListViewModel.ShowFavoritesOnly = false;
        NotesListViewModel.TypeFilter = NoteType.Todo;
    }

    /// <summary>
    /// Shows only notes of type Note.
    /// </summary>
    [RelayCommand]
    public void ShowNotes()
    {
        IsSettingsOpen = false;
        SelectedNavView = NavigationView.Notes;
        NotesListViewModel.SearchQuery = string.Empty;
        NotesListViewModel.ShowFavoritesOnly = false;
        NotesListViewModel.TypeFilter = NoteType.Note;
    }

    /// <summary>
    /// Shows only notes marked as favorite, regardless of their type.
    /// </summary>
    [RelayCommand]
    public void ShowFavorites()
    {
        IsSettingsOpen = false;
        SelectedNavView = NavigationView.Favorites;
        NotesListViewModel.SearchQuery = string.Empty;
        NotesListViewModel.ShowFavoritesOnly = true;
        NotesListViewModel.TypeFilter = null;
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
    /// Requests that the notes list window close. The app intercepts this and hides the
    /// window to the system tray instead of exiting; only the tray icon's "Exit" command quits.
    /// </summary>
    [RelayCommand]
    public void CloseWindow()
    {
        _mainWindowService.RequestClose();
    }

    /// <summary>
    /// Opens the Settings page within the main window's content area, without affecting
    /// the notes list state or any currently open sticky note windows.
    /// </summary>
    [RelayCommand]
    public void OpenSettings()
    {
        IsSettingsOpen = true;
    }

}
