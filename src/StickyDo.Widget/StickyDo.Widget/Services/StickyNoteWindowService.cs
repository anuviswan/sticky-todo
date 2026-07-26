using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Utilities;
using StickyDo.Widget.ViewModels;
using StickyDo.Widget.Views;

namespace StickyDo.Widget.Services;

/// <summary>
/// Service for managing sticky note window creation and lifecycle.
/// Handles UI concerns that ViewModels should not directly manage.
/// Maintains pure MVVM by delegating UI operations to this service.
/// </summary>
public class StickyNoteWindowService : IStickyNoteWindowService
{
    private readonly StickyNoteService _stickyNoteService;
    private readonly StickyNoteTaskService _stickyNoteTaskService;
    private readonly WindowManager _windowManager;
    private readonly IDialogService _dialogService;
    private readonly Lazy<IStickyNoteCreationService> _creationService;
    private readonly IPersistenceService _persistenceService;
    private readonly IMessenger _messenger;
    private readonly IWindowService _windowService;

    public StickyNoteWindowService(
        StickyNoteService stickyNoteService,
        StickyNoteTaskService stickyNoteTaskService,
        WindowManager windowManager,
        IDialogService dialogService,
        Lazy<IStickyNoteCreationService> creationService,
        IPersistenceService persistenceService,
        IMessenger messenger,
        IWindowService windowService)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(stickyNoteTaskService);
        ArgumentNullException.ThrowIfNull(windowManager);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(creationService);
        ArgumentNullException.ThrowIfNull(persistenceService);
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(windowService);
        _stickyNoteService = stickyNoteService;
        _stickyNoteTaskService = stickyNoteTaskService;
        _windowManager = windowManager;
        _dialogService = dialogService;
        _creationService = creationService;
        _persistenceService = persistenceService;
        _messenger = messenger;
        _windowService = windowService;
    }

    /// <summary>
    /// Opens or focuses a sticky note window for the given note ID.
    /// </summary>
    public async Task OpenNoteWindowAsync(Guid noteId)
    {
        try
        {
            // Check if window is already open (or being opened by a concurrent call for the same note)
            if (_windowManager.IsNoteWindowOpen(noteId))
            {
                var existingWindow = _windowManager.GetNoteWindow(noteId);
                if (existingWindow != null && existingWindow.IsVisible)
                {
                    existingWindow.Activate();
                    existingWindow.Focus();
                }
                return;
            }

            // Register the window before the first await below, so a concurrent call for the
            // same note (e.g. a double double-click) sees it as already open instead of racing
            // past the check above and creating a second window for the same note.
            var window = new StickyNoteWindow();
            _windowManager.RegisterNoteWindow(noteId, window);

            try
            {
                var viewModel = new StickyNoteWindowViewModel(
                    _stickyNoteService,
                    _stickyNoteTaskService,
                    _dialogService,
                    _creationService.Value,
                    _persistenceService,
                    _messenger,
                    _windowService);

                await viewModel.LoadNoteAsync(noteId);

                window.DataContext = viewModel;

                // Close the window itself (not the shared main window) when the user requests it
                viewModel.CloseRequested += (s, e) => window.Close();

                // Restore window state: prefer the in-memory state from this session, then fall
                // back to the position persisted on disk from the last time this note was closed.
                var savedState = _windowManager.GetSavedNoteWindowState(noteId);
                if (savedState != null)
                {
                    window.Left = savedState.Left;
                    window.Top = savedState.Top;
                    window.Width = savedState.Width;
                    window.Height = savedState.Height;
                }
                else
                {
                    var note = await _stickyNoteService.GetNoteByIdAsync(noteId);
                    if (note?.WindowLeft is { } left && note.WindowTop is { } top)
                    {
                        window.Left = left;
                        window.Top = top;
                        window.Width = note.WindowWidth ?? window.Width;
                        window.Height = note.WindowHeight ?? window.Height;
                    }
                    else
                    {
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    }
                }

                window.Closed += async (s, e) =>
                {
                    _windowManager.SaveNoteWindowState(noteId, window.Left, window.Top, window.Width, window.Height);
                    _windowManager.UnregisterNoteWindow(noteId);

                    try
                    {
                        // The note may have just been permanently deleted (e.g. via the Delete Note
                        // menu action), in which case it no longer exists to update - skip re-persisting it.
                        var noteStillExists = await _stickyNoteService.GetNoteByIdAsync(noteId) is not null;
                        if (noteStillExists)
                        {
                            await _stickyNoteService.SetNoteOpenStateAsync(noteId, false);
                            await _stickyNoteService.UpdateNoteWindowBoundsAsync(noteId, window.Left, window.Top, window.Width, window.Height);
                            await _persistenceService.SaveAllDirtyNotesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogException(ex, nameof(OpenNoteWindowAsync));
                    }
                };

                await _stickyNoteService.SetNoteOpenStateAsync(noteId, true);
                await _persistenceService.SaveAllDirtyNotesAsync();

                window.Show();
            }
            catch
            {
                // Loading/showing failed before the window's own Closed handler could take over
                // cleanup - unregister here so the note isn't stuck looking "open" forever.
                _windowManager.UnregisterNoteWindow(noteId);
                throw;
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(OpenNoteWindowAsync));
            await _dialogService.ShowMessageAsync("Open Note Error", $"Error opening note: {ex.Message}", MessageBoxImage.Error);
        }
    }
}
