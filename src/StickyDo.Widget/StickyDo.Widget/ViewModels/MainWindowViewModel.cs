using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickyDo.Domain.Models;
using StickyDo.Domain.Services;
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
/// ViewModel for the main application window managing the sticky notes list.
/// Pure MVVM - delegates window management to IStickyNoteWindowService and IWindowService.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly StickyNoteService _stickyNoteService;
    private readonly IStickyNoteWindowService _windowService;
    private readonly IDialogService _dialogService;
    private readonly IWindowService _mainWindowService;
    private ObservableCollection<StickyNoteItemViewModel> _allNotes = new();

    [ObservableProperty]
    private NavigationView selectedNavView = NavigationView.AllNotes;

    [ObservableProperty]
    private ObservableCollection<StickyNoteItemViewModel> filteredNotes = new();

    [ObservableProperty]
    private string searchQuery = string.Empty;

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
        UpdateNoteCount();
    }

    [ObservableProperty]
    private StickyNoteItemViewModel? selectedNote;

    [ObservableProperty]
    private string syncStatus = AppStrings.SyncedStatus;

    [ObservableProperty]
    private string noteCountDisplay = AppStrings.ZeroNotes;

    [ObservableProperty]
    private string lastSyncDisplay = AppStrings.JustNow;

    public MainWindowViewModel(
        StickyNoteService stickyNoteService,
        IStickyNoteWindowService windowService,
        IDialogService dialogService,
        IWindowService mainWindowService)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(mainWindowService);
        _stickyNoteService = stickyNoteService;
        _windowService = windowService;
        _dialogService = dialogService;
        _mainWindowService = mainWindowService;
    }

    /// <summary>
    /// Loads all sticky notes from the repository.
    /// </summary>
    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        try
        {
            SyncStatus = AppStrings.SyncingStatus;
            var notes = await _stickyNoteService.GetAllNotesAsync();

            _allNotes.Clear();
            foreach (var note in notes.OrderByDescending(n => n.UpdatedAt))
            {
                _allNotes.Add(new StickyNoteItemViewModel
                {
                    Id = note.Id,
                    Title = note.Title,
                    Status = note.Status.ToString(),
                    LastModified = note.UpdatedAt,
                    ColorArgb = note.ColorArgb ?? 0xFFFFCC07
                });
            }

            ApplyFilter();
            UpdateNoteCount();
            SyncStatus = AppStrings.SyncedStatus;
            LastSyncDisplay = AppStrings.JustNow;
        }
        catch (Exception ex)
        {
            SyncStatus = AppStrings.ErrorStatus;
            LoggerHelper.LogException(ex, nameof(LoadNotesAsync));
            await _dialogService.ShowMessageAsync(
                AppStrings.LoadErrorTitle,
                string.Format(AppStrings.ErrorLoadingNotes, ex.Message),
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Creates a new sticky note and opens it in a floating window.
    /// </summary>
    [RelayCommand]
    public async Task CreateNoteAsync()
    {
        try
        {
            var noteNumber = await _stickyNoteService.GetNextNoteNumberAsync();
            var noteTitle = $"Note {noteNumber}";
            var noteId = await _stickyNoteService.CreateNoteAsync(noteTitle);

            await _windowService.OpenNoteWindowAsync(noteId);
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(CreateNoteAsync));
            await _dialogService.ShowMessageAsync(
                AppStrings.LoadErrorTitle,
                string.Format(AppStrings.ErrorLoadingNotes, ex.Message),
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Deletes the selected sticky note (placeholder for Phase 2).
    /// </summary>
    [RelayCommand]
    public async Task DeleteNoteAsync()
    {
        if (SelectedNote is null)
            return;

        // Placeholder for Phase 2 implementation
        await Task.CompletedTask;
    }

    /// <summary>
    /// Searches notes based on the search query.
    /// </summary>
    [RelayCommand]
    public async Task SearchNotesAsync()
    {
        ApplyFilter();
        UpdateNoteCount();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Shows all notes by clearing search filter.
    /// </summary>
    [RelayCommand]
    public void ShowAllNotes()
    {
        SelectedNavView = NavigationView.AllNotes;
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    /// <summary>
    /// Shows favorite notes (placeholder for Phase 2).
    /// </summary>
    [RelayCommand]
    public void ShowFavorites()
    {
        SelectedNavView = NavigationView.Favorites;
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    /// <summary>
    /// Shows trash notes (placeholder for Phase 2).
    /// </summary>
    [RelayCommand]
    public void ShowTrash()
    {
        SelectedNavView = NavigationView.Trash;
        SearchQuery = string.Empty;
        ApplyFilter();
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

    private void ApplyFilter()
    {
        var filtered = _allNotes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.ToLower();
            filtered = filtered.Where(n =>
                n.Title.ToLower().Contains(query)
            );
        }

        FilteredNotes.Clear();
        foreach (var note in filtered)
        {
            FilteredNotes.Add(note);
        }

        UpdateNoteCount();
    }

    private void UpdateNoteCount()
    {
        var count = FilteredNotes.Count;
        NoteCountDisplay = count switch
        {
            0 => AppStrings.ZeroNotes,
            1 => AppStrings.SingleNote,
            _ => string.Format(AppStrings.MultipleNotesFormat, count)
        };
    }

}
