using System.Windows;
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
    private readonly IWindowService _windowService;
    private readonly IStickyNoteWindowCoordinator _windowCoordinator;

    public StickyNoteWindowService(
        StickyNoteService stickyNoteService,
        WindowManager windowManager,
        IDialogService dialogService,
        IWindowService windowService,
        IStickyNoteWindowCoordinator windowCoordinator)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(windowManager);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(windowCoordinator);
        _stickyNoteService = stickyNoteService;
        _windowManager = windowManager;
        _dialogService = dialogService;
        _windowService = windowService;
        _windowCoordinator = windowCoordinator;
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
                _windowService,
                _windowCoordinator);

            await viewModel.LoadNoteAsync(noteId);

            window.DataContext = viewModel;
            _windowManager.RegisterNoteWindow(noteId, window);

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

            window.Closed += (s, e) =>
            {
                _windowManager.SaveNoteWindowState(noteId, window.Left, window.Top, window.Width, window.Height);
                _windowManager.UnregisterNoteWindow(noteId);
            };

            window.Show();
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(OpenNoteWindowAsync));
            await _dialogService.ShowMessageAsync("Open Note Error", $"Error opening note: {ex.Message}", MessageBoxImage.Error);
        }
    }
}
