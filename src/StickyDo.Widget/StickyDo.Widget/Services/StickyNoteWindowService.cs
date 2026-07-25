using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Utilities;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Services;

/// <summary>
/// Service for managing sticky note window creation and lifecycle.
/// Handles UI concerns that ViewModels should not directly manage.
/// Maintains pure MVVM by delegating UI operations to this service.
/// </summary>
public class StickyNoteWindowService : IStickyNoteWindowService
{
    private readonly StickyNoteService _stickyNoteService;
    private readonly WindowManager _windowManager;
    private readonly IDialogService _dialogService;
    private readonly Lazy<IStickyNoteCreationService> _creationService;
    private readonly PersistenceService _persistenceService;
    private readonly IMessenger _messenger;

    public StickyNoteWindowService(
        StickyNoteService stickyNoteService,
        WindowManager windowManager,
        IDialogService dialogService,
        Lazy<IStickyNoteCreationService> creationService,
        PersistenceService persistenceService,
        IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(windowManager);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(creationService);
        ArgumentNullException.ThrowIfNull(persistenceService);
        ArgumentNullException.ThrowIfNull(messenger);
        _stickyNoteService = stickyNoteService;
        _windowManager = windowManager;
        _dialogService = dialogService;
        _creationService = creationService;
        _persistenceService = persistenceService;
        _messenger = messenger;
    }

    /// <summary>
    /// Opens or focuses a sticky note window for the given note ID.
    /// </summary>
    public async Task OpenNoteWindowAsync(Guid noteId)
    {
        try
        {
            // Check if window is already open
            if (_windowManager.IsNoteWindowOpen(noteId))
            {
                var existingWindow = _windowManager.GetNoteWindow(noteId);
                if (existingWindow != null)
                {
                    existingWindow.Activate();
                    existingWindow.Focus();
                }
                return;
            }

            // Create new window
            var window = new StickyNoteWindow();
            var viewModel = new StickyNoteWindowViewModel(
                _stickyNoteService,
                _dialogService,
                _creationService.Value,
                _persistenceService,
                _messenger);

            await viewModel.LoadNoteAsync(noteId);

            window.DataContext = viewModel;
            _windowManager.RegisterNoteWindow(noteId, window);

            // Close the window itself (not the shared main window) when the user requests it
            viewModel.CloseRequested += (s, e) => window.Close();

            // Restore window state if available
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
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            window.Closed += async (s, e) =>
            {
                _windowManager.SaveNoteWindowState(noteId, window.Left, window.Top, window.Width, window.Height);
                _windowManager.UnregisterNoteWindow(noteId);

                try
                {
                    await _stickyNoteService.SetNoteOpenStateAsync(noteId, false);
                    await _persistenceService.SaveAllDirtyNotesAsync();
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
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(OpenNoteWindowAsync));
            await _dialogService.ShowMessageAsync("Open Note Error", $"Error opening note: {ex.Message}", MessageBoxImage.Error);
        }
    }
}
