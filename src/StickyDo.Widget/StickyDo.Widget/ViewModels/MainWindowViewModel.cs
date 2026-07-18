using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickyDo.Domain.Models;
using StickyDo.Domain.Services;
using System.Windows.Media;

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
    private string syncStatus = "Synced";

    [ObservableProperty]
    private string noteCountDisplay = "0 notes";

    [ObservableProperty]
    private string lastSyncDisplay = "Just now";

    public MainWindowViewModel(StickyNoteService stickyNoteService)
    {
        _stickyNoteService = stickyNoteService ?? throw new ArgumentNullException(nameof(stickyNoteService));
    }

    /// <summary>
    /// Loads all sticky notes from the repository.
    /// </summary>
    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        try
        {
            SyncStatus = "Syncing...";
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
                    ColorBrush = GetColorBrushForNote(note)
                });
            }

            ApplyFilter();
            UpdateNoteCount();
            SyncStatus = "Synced";
            LastSyncDisplay = "Just now";
        }
        catch (Exception ex)
        {
            SyncStatus = "Error";
            System.Windows.MessageBox.Show($"Error loading notes: {ex.Message}", "Load Error");
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
        NoteCountDisplay = $"{count} {(count == 1 ? "note" : "notes")}";
    }

    private static Brush GetColorBrushForNote(StickyNote note)
    {
        var argb = note.ColorArgb ?? 0xFFFFCC07;
        var color = System.Windows.Media.Color.FromArgb(
            (byte)((argb >> 24) & 0xFF),
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF)
        );
        return new SolidColorBrush(color);
    }
}
