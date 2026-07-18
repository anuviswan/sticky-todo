using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickyDo.Domain.Models;
using StickyDo.Domain.Services;
using StickyDo.Widget.Resources;
using StickyDo.Widget.Utilities;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for the main application window managing the sticky notes list.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly StickyNoteService _stickyNoteService;
    private ObservableCollection<StickyNoteItemViewModel> _allNotes = new();

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

    public MainWindowViewModel(StickyNoteService stickyNoteService)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        _stickyNoteService = stickyNoteService;
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
                    Content = note.Content,
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
            System.Windows.MessageBox.Show(
                string.Format(AppStrings.ErrorLoadingNotes, ex.Message),
                AppStrings.LoadErrorTitle);
        }
    }

    /// <summary>
    /// Creates a new sticky note (placeholder for Phase 2).
    /// </summary>
    [RelayCommand]
    public async Task CreateNoteAsync()
    {
        // Placeholder for Phase 2 implementation
        await Task.CompletedTask;
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
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    /// <summary>
    /// Shows favorite notes (placeholder for Phase 2).
    /// </summary>
    [RelayCommand]
    public void ShowFavorites()
    {
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    /// <summary>
    /// Shows trash notes (placeholder for Phase 2).
    /// </summary>
    [RelayCommand]
    public void ShowTrash()
    {
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    /// <summary>
    /// Minimizes the application window.
    /// </summary>
    [RelayCommand]
    public void MinimizeWindow(object? parameter)
    {
        if (parameter is Window window)
            window.WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// Closes the application window.
    /// </summary>
    [RelayCommand]
    public void CloseWindow(object? parameter)
    {
        if (parameter is Window window)
            window.Close();
    }

    private void ApplyFilter()
    {
        var filtered = _allNotes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.ToLower();
            filtered = filtered.Where(n =>
                n.Title.ToLower().Contains(query) ||
                n.Content.ToLower().Contains(query)
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
