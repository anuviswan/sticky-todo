using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickyDo.Domain.Models;
using StickyDo.Domain.Services;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for the main application window managing the sticky notes list.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly StickyNoteService _stickyNoteService;

    [ObservableProperty]
    private ObservableCollection<StickyNoteItemViewModel> notes = new();

    [ObservableProperty]
    private string searchQuery = string.Empty;

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

            Notes.Clear();
            foreach (var note in notes.OrderByDescending(n => n.UpdatedAt))
            {
                Notes.Add(new StickyNoteItemViewModel
                {
                    Id = note.Id,
                    Title = note.Title,
                    Content = note.Content,
                    Status = note.Status.ToString(),
                    LastModified = note.UpdatedAt,
                    ColorBrush = GetColorBrushForNote(note)
                });
            }

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
    /// Searches notes based on the search query (placeholder for Phase 2).
    /// </summary>
    [RelayCommand]
    public async Task SearchNotesAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadNotesAsync();
            return;
        }

        // Placeholder for Phase 2 implementation
        await Task.CompletedTask;
    }

    private void UpdateNoteCount()
    {
        var count = Notes.Count;
        NoteCountDisplay = $"{count} {(count == 1 ? "note" : "notes")}";
    }

    private static System.Windows.Media.Brush GetColorBrushForNote(StickyNote note)
    {
        // Placeholder - use default color
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7));
    }
}
