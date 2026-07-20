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
/// ViewModel for the notes list view, managing the display and filtering of sticky notes.
/// </summary>
public partial class NotesListViewModel : ObservableObject
{
    private readonly StickyNoteService _stickyNoteService;
    private readonly IStickyNoteWindowService _windowService;
    private readonly IDialogService _dialogService;
    private ObservableCollection<StickyNoteItemViewModel> _allNotes = new();

    [ObservableProperty]
    private ObservableCollection<StickyNoteItemViewModel> filteredNotes = new();

    [ObservableProperty]
    private string searchQuery = string.Empty;

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    public NotesListViewModel(
        StickyNoteService stickyNoteService,
        IStickyNoteWindowService windowService,
        IDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(dialogService);
        _stickyNoteService = stickyNoteService;
        _windowService = windowService;
        _dialogService = dialogService;
    }

    /// <summary>
    /// Loads all sticky notes from the repository.
    /// </summary>
    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        try
        {
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
        }
        catch (Exception ex)
        {
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
    }
}
