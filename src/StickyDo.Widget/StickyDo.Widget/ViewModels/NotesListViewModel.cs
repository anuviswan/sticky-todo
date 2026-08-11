using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StickyDo.Domain.Constants;
using StickyDo.Domain.Models;
using StickyDo.Domain.Models.RichText;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Messages;
using StickyDo.Widget.Utilities;
using AppResources = StickyDo.Widget.Resources.Resources;

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
    private readonly ISettingsRepository _settingsRepository;
    private readonly IPersistenceService _persistenceService;
    private readonly List<StickyNoteItemViewModel> _allNotes = new();

    [ObservableProperty]
    private ObservableCollection<NoteColumnViewModel> columns = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearchActive))]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private bool showFavoritesOnly;

    /// <summary>When set, restricts the list to notes of this type (e.g. Todos-only or Notes-only sections).</summary>
    [ObservableProperty]
    private NoteType? typeFilter;

    /// <summary>Whether a non-blank search query is currently active, used to drive the search empty state.</summary>
    public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchQuery);

    /// <summary>Total number of Todo-type items, independent of the current filter.</summary>
    public int TodoCount => _allNotes.Count(n => n.Type == NoteType.Todo);

    /// <summary>Total number of Note-type items, independent of the current filter.</summary>
    public int NoteCount => _allNotes.Count(n => n.Type == NoteType.Note);

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnShowFavoritesOnlyChanged(bool value)
    {
        ApplyFilter();
    }

    partial void OnTypeFilterChanged(NoteType? value)
    {
        ApplyFilter();
    }

    public NotesListViewModel(
        StickyNoteService stickyNoteService,
        IStickyNoteWindowService windowService,
        IDialogService dialogService,
        IMessenger messenger,
        ISettingsRepository settingsRepository,
        IPersistenceService persistenceService)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(settingsRepository);
        ArgumentNullException.ThrowIfNull(persistenceService);
        _stickyNoteService = stickyNoteService;
        _windowService = windowService;
        _dialogService = dialogService;
        _messenger = messenger;
        _settingsRepository = settingsRepository;
        _persistenceService = persistenceService;

        _messenger.Register<StickyNoteChangedMessage>(this, async (recipient, message) =>
            await ((NotesListViewModel)recipient).OnNoteChangedAsync(message));

        _messenger.Register<NotesImportedMessage>(this, async (recipient, message) =>
            await ((NotesListViewModel)recipient).LoadNotesAsync());
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
                AppResources.LoadErrorTitle,
                string.Format(AppResources.ErrorLoadingNotes, ex.Message),
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Creates a new sticky note and opens it in a floating window. The created note's type
    /// follows the current section: Notes (<see cref="TypeFilter"/> == <see cref="NoteType.Note"/>)
    /// creates a free-form Note; every other section (Todos, All Notes, Favorites) creates a Todo.
    /// </summary>
    [RelayCommand]
    public async Task CreateNoteAsync()
    {
        try
        {
            var noteNumber = await _stickyNoteService.GetNextNoteNumberAsync();
            var noteTitle = $"Note {noteNumber}";
            var type = TypeFilter == NoteType.Note ? NoteType.Note : NoteType.Todo;
            var settings = await _settingsRepository.LoadAsync();
            var noteId = await _stickyNoteService.CreateNoteAsync(noteTitle, settings.DefaultNoteColor, type);

            // Open the window first - it adds the default "First Task" during load. Notifying
            // the list only after that completes ensures its card reflects that task, instead
            // of caching a stale zero-task snapshot that nothing ever refreshes afterward.
            await _windowService.OpenNoteWindowAsync(noteId);

            _messenger.Send(new StickyNoteChangedMessage(noteId, StickyNoteChangeType.Created));
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(CreateNoteAsync));
            await _dialogService.ShowMessageAsync(
                AppResources.LoadErrorTitle,
                string.Format(AppResources.ErrorLoadingNotes, ex.Message),
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opens a note as a floating sticky note window, e.g. when double-clicked in the list
    /// or via the "Open" item in a note card's right-click context menu.
    /// </summary>
    [RelayCommand]
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
                AppResources.LoadErrorTitle,
                string.Format(AppResources.ErrorLoadingNotes, ex.Message),
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

        var (firstTaskPreview, firstTaskPreviewFormatting) =
            RichTextPreviewBuilder.BuildPreview(firstTask?.Title, firstTask?.TitleFormatting, ContentPreviewMaxWords);
        var (contentPreview, contentPreviewFormatting) =
            RichTextPreviewBuilder.BuildPreview(note.Content, note.ContentFormatting, ContentPreviewMaxWords);

        return new StickyNoteItemViewModel
        {
            Id = note.Id,
            Title = note.Title,
            LastModified = note.UpdatedAt,
            ColorArgb = note.ColorArgb ?? ColorPalette.GetDefaultColor(),
            Type = note.Type,
            HasTasks = firstTask is not null,
            IsFavorite = note.IsFavorite,
            FirstTaskTitle = firstTaskPreview,
            FirstTaskTitleFormatting = firstTaskPreviewFormatting,
            FirstTaskCompleted = firstTask?.IsCompleted ?? false,
            RemainingTaskCount = Math.Max(0, orderedTasks.Count - 1),
            TaskTitles = orderedTasks.Select(t => t.Title).ToList(),
            Content = note.Content ?? string.Empty,
            ContentPreview = contentPreview,
            ContentPreviewFormatting = contentPreviewFormatting
        };
    }

    /// <summary>
    /// Card-friendly preview length for a note's display text (a Todo's first task title, or a
    /// free-form Note's content): the first few words, with "..." appended when there's more. See
    /// <see cref="RichTextPreviewBuilder"/> for how this is built alongside remapped formatting.
    /// </summary>
    private const int ContentPreviewMaxWords = 12;

    /// <summary>
    /// Prompts for confirmation - warning that the action is permanent and cannot be undone -
    /// then permanently deletes the note, e.g. when dropped onto the Trash icon. Reuses the same
    /// StickyNoteService.DeleteNoteAsync + Deleted broadcast as the note window's own "Delete
    /// Note" menu action (StickyNoteWindowViewModel.DeleteNoteAsync); this is just a second
    /// trigger for the same irreversible operation, not a soft delete/Trash storage.
    /// Exposed as a command (rather than called directly) so SidebarNavigation's Trash drop
    /// target can invoke it via binding without StickyDo.Widget.Controls needing a reference to
    /// this ViewModel's project.
    /// </summary>
    [RelayCommand]
    public async Task RequestDeleteNoteAsync(Guid noteId)
    {
        var note = _allNotes.FirstOrDefault(n => n.Id == noteId);
        if (note is null)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            AppResources.DragDeleteNote_ConfirmTitle,
            string.Format(AppResources.DragDeleteNote_ConfirmMessage, note.Title));

        if (!confirmed)
            return;

        try
        {
            await _stickyNoteService.DeleteNoteAsync(noteId);
            _messenger.Send(new StickyNoteChangedMessage(noteId, StickyNoteChangeType.Deleted));
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(RequestDeleteNoteAsync));
            await _dialogService.ShowMessageAsync(
                AppResources.DragDeleteNote_ErrorTitle,
                string.Format(AppResources.DragDeleteNote_ErrorMessage, ex.Message),
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Toggles the favourite state of a note and persists it, e.g. when the star icon on a note
    /// card in the list is clicked. Reverts the icon back to its previous state if persisting
    /// fails, so the UI never shows a favourite status that wasn't actually saved.
    /// </summary>
    [RelayCommand]
    public async Task ToggleFavoriteAsync(Guid noteId)
    {
        var note = _allNotes.FirstOrDefault(n => n.Id == noteId);
        if (note is null)
            return;

        var previousValue = note.IsFavorite;
        note.IsFavorite = !previousValue;
        ApplyFilter();

        try
        {
            await _stickyNoteService.SetNoteFavoriteAsync(noteId, note.IsFavorite);

            // The Notes List has no autosave loop of its own (unlike an open note window), so
            // without this the toggle would only reach disk incidentally - e.g. if a note window
            // happens to be open and autosaving, or on app exit. Flushing here guarantees the
            // change is durable immediately, matching StickyNoteWindowViewModel.SaveAsync.
            await _persistenceService.SaveAllDirtyNotesAsync();

            _messenger.Send(new StickyNoteChangedMessage(noteId, StickyNoteChangeType.Updated));
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(ToggleFavoriteAsync));
            note.IsFavorite = previousValue;
            ApplyFilter();
            await _dialogService.ShowMessageAsync(
                AppResources.Favorite_ErrorTitle,
                string.Format(AppResources.Favorite_ErrorMessage, ex.Message),
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Applies the search filter and regroups the results into color-based columns,
    /// ordered by palette position, with notes sorted by Last Updated descending.
    /// The grouped result is fully materialized before <see cref="Columns"/> is touched, so if
    /// filtering fails, the previously displayed notes remain visible instead of being cleared.
    /// </summary>
    private void ApplyFilter()
    {
        try
        {
            var filtered = _allNotes.AsEnumerable();

            if (ShowFavoritesOnly)
            {
                filtered = filtered.Where(n => n.IsFavorite);
            }

            if (TypeFilter is not null)
            {
                filtered = filtered.Where(n => n.Type == TypeFilter.Value);
            }

            var query = SearchQuery.Trim();
            if (query.Length > 0)
            {
                filtered = filtered.Where(n =>
                    n.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    n.TaskTitles.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    n.Content.Contains(query, StringComparison.OrdinalIgnoreCase)
                );
            }

            var grouped = filtered
                .GroupBy(n => n.ColorArgb)
                .OrderBy(g => Array.IndexOf(ColorPalette.Colors, g.Key))
                .ToList();

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
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(ApplyFilter));
            _ = _dialogService.ShowMessageAsync(
                AppResources.Search_ErrorTitle,
                string.Format(AppResources.Search_ErrorMessage, ex.Message),
                System.Windows.MessageBoxImage.Error);
        }
    }
}
