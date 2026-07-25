using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StickyDo.Domain.Constants;
using StickyDo.Domain.Models;
using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Messages;
using StickyDo.Widget.Resources;
using StickyDo.Widget.Services;
using StickyDo.Widget.Utilities;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for the notes list view, managing the display and filtering of sticky notes.
/// Notes are grouped into color-based columns, sorted by Last Updated within each column.
/// </summary>
public partial class NotesListViewModel : ObservableObject
{
    private readonly StickyNoteService _stickyNoteService;
    private readonly IStickyNoteWindowService _windowService;
    private readonly IDialogService _dialogService;
    private readonly IMessenger _messenger;
    private readonly List<StickyNoteItemViewModel> _allNotes = new();

    [ObservableProperty]
    private ObservableCollection<NoteColumnViewModel> columns = new();

    [ObservableProperty]
    private string searchQuery = string.Empty;

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    public NotesListViewModel(
        StickyNoteService stickyNoteService,
        IStickyNoteWindowService windowService,
        IDialogService dialogService,
        IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(messenger);
        _stickyNoteService = stickyNoteService;
        _windowService = windowService;
        _dialogService = dialogService;
        _messenger = messenger;

        _messenger.Register<StickyNoteChangedMessage>(this, async (recipient, message) =>
            await ((NotesListViewModel)recipient).OnNoteChangedAsync(message));
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
            foreach (var note in notes)
            {
                _allNotes.Add(ToItemViewModel(note));
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
            _messenger.Send(new StickyNoteChangedMessage(noteId, StickyNoteChangeType.Created));

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
    /// Opens a note as a floating sticky note window, e.g. when double-clicked in the list.
    /// </summary>
    public async Task OpenNoteAsync(Guid noteId)
    {
        try
        {
            await _windowService.OpenNoteWindowAsync(noteId);
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(OpenNoteAsync));
            await _dialogService.ShowMessageAsync(
                AppStrings.LoadErrorTitle,
                string.Format(AppStrings.ErrorLoadingNotes, ex.Message),
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles a note being created, updated, or deleted elsewhere in the app,
    /// keeping the notes list current without requiring an app restart.
    /// </summary>
    private async Task OnNoteChangedAsync(StickyNoteChangedMessage message)
    {
        try
        {
            if (message.ChangeType == StickyNoteChangeType.Deleted)
            {
                _allNotes.RemoveAll(n => n.Id == message.NoteId);
            }
            else
            {
                var note = await _stickyNoteService.GetNoteByIdAsync(message.NoteId);
                if (note is null)
                    return;

                _allNotes.RemoveAll(n => n.Id == message.NoteId);
                _allNotes.Add(ToItemViewModel(note));
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(OnNoteChangedAsync));
        }
    }

    private static StickyNoteItemViewModel ToItemViewModel(StickyNote note)
    {
        var orderedTasks = note.Tasks.OrderBy(t => t.Order).ToList();
        var firstTask = orderedTasks.FirstOrDefault();

        return new StickyNoteItemViewModel
        {
            Id = note.Id,
            Title = note.Title,
            LastModified = note.UpdatedAt,
            ColorArgb = note.ColorArgb ?? ColorPalette.GetDefaultColor(),
            HasTasks = firstTask is not null,
            FirstTaskTitle = firstTask?.Title ?? string.Empty,
            FirstTaskCompleted = firstTask?.IsCompleted ?? false,
            RemainingTaskCount = Math.Max(0, orderedTasks.Count - 1)
        };
    }

    /// <summary>
    /// Applies the search filter and regroups the results into color-based columns,
    /// ordered by palette position, with notes sorted by Last Updated descending.
    /// </summary>
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

        var grouped = filtered
            .GroupBy(n => n.ColorArgb)
            .OrderBy(g => Array.IndexOf(ColorPalette.Colors, g.Key));

        Columns.Clear();
        foreach (var group in grouped)
        {
            var column = new NoteColumnViewModel { ColorArgb = group.Key };
            foreach (var note in group.OrderByDescending(n => n.LastModified))
            {
                column.Notes.Add(note);
            }

            Columns.Add(column);
        }

        // ItemsControl.ItemsSource observes CollectionChanged directly, but the empty-state
        // Visibility binding goes through a converter and only refreshes on PropertyChanged.
        OnPropertyChanged(nameof(Columns));
    }
}
