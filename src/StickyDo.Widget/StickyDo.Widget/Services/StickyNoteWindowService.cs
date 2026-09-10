using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using StickyDo.Domain.Services;
using StickyDo.Widget.Behaviors;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Messages;
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
    private readonly IScreenProvider _screenProvider;

    public StickyNoteWindowService(
        StickyNoteService stickyNoteService,
        StickyNoteTaskService stickyNoteTaskService,
        WindowManager windowManager,
        IDialogService dialogService,
        Lazy<IStickyNoteCreationService> creationService,
        IPersistenceService persistenceService,
        IMessenger messenger,
        IWindowService windowService,
        IScreenProvider screenProvider)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(stickyNoteTaskService);
        ArgumentNullException.ThrowIfNull(windowManager);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(creationService);
        ArgumentNullException.ThrowIfNull(persistenceService);
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(screenProvider);
        _stickyNoteService = stickyNoteService;
        _stickyNoteTaskService = stickyNoteTaskService;
        _windowManager = windowManager;
        _dialogService = dialogService;
        _creationService = creationService;
        _persistenceService = persistenceService;
        _messenger = messenger;
        _windowService = windowService;
        _screenProvider = screenProvider;

        _messenger.Register<StickyNoteChangedMessage>(this, (recipient, message) =>
            ((StickyNoteWindowService)recipient).OnNoteChanged(message));
    }

    /// <summary>
    /// Closes a note's floating window if it's open and the note was just permanently deleted
    /// elsewhere (e.g. dragged onto the Trash icon from the Notes List) - otherwise the window
    /// would keep showing a note that no longer exists in storage.
    /// </summary>
    private void OnNoteChanged(StickyNoteChangedMessage message)
    {
        if (message.ChangeType != StickyNoteChangeType.Deleted)
            return;

        if (_windowManager.GetNoteWindow(message.NoteId) is not { } window)
            return;

        try
        {
            window.Close();
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(OnNoteChanged));
        }
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

                // Close the window itself (not the shared main window) when the user requests it.
                // Guarded because DeleteNoteAsync both broadcasts a Deleted message (which
                // OnNoteChanged above reacts to by closing this same window) and raises
                // CloseRequested - without the guard this would call Close() a second time.
                viewModel.CloseRequested += (s, e) =>
                {
                    if (_windowManager.IsNoteWindowOpen(noteId))
                        window.Close();
                };

                // Restore window state: prefer the in-memory state from this session, then fall
                // back to the position persisted on disk from the last time this note was closed.
                var savedBounds = ToBounds(_windowManager.GetSavedNoteWindowState(noteId))
                    ?? ToBounds(await _stickyNoteService.GetNoteByIdAsync(noteId), window);

                // The saved position comes from whatever display layout the note was last closed on.
                // If that monitor is gone (a docked laptop opened undocked, or a note restored onto a
                // narrower machine) the window would open completely off-screen and look like it never
                // opened at all, so pull it back onto a connected monitor - see issue #183.
                // Only set when the placement had to be corrected: the position the user actually
                // chose, plus the corrected position we put the window at instead. On close the
                // original is written back unless the user has since moved the window, so
                // reconnecting the missing monitor restores their original layout.
                Rect? uncorrectedBounds = null;
                Rect? correctedBounds = null;

                if (savedBounds is { } bounds)
                {
                    var placement = NoteWindowPlacementCalculator.EnsureVisible(bounds, _screenProvider.GetWorkAreasInDips());

                    window.Left = placement.Bounds.Left;
                    window.Top = placement.Bounds.Top;
                    window.Width = placement.Bounds.Width;
                    window.Height = placement.Bounds.Height;

                    if (placement.WasCorrected)
                    {
                        uncorrectedBounds = bounds;
                        correctedBounds = placement.Bounds;
                    }
                }
                else
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }

                window.Closed += async (s, e) =>
                {
                    var closingBounds = new Rect(window.Left, window.Top, window.Width, window.Height);

                    // Comparing the closing bounds against what we placed the window at is more
                    // reliable than watching LocationChanged/SizeChanged, which WPF can also raise
                    // during a transparent window's own layout passes.
                    var boundsToPersist = uncorrectedBounds is { } uncorrected && IsUnmovedFrom(closingBounds, correctedBounds)
                        ? uncorrected
                        : closingBounds;

                    _windowManager.SaveNoteWindowState(noteId, boundsToPersist.Left, boundsToPersist.Top, boundsToPersist.Width, boundsToPersist.Height);
                    _windowManager.UnregisterNoteWindow(noteId);

                    try
                    {
                        // The note may have just been permanently deleted (e.g. via the Delete Note
                        // menu action), in which case it no longer exists to update - skip re-persisting it.
                        var noteStillExists = await _stickyNoteService.GetNoteByIdAsync(noteId) is not null;
                        if (noteStillExists)
                        {
                            // If the whole app is exiting (tray "Exit" or a Windows session ending),
                            // this Closed event is part of the mass window-close that happens during
                            // shutdown, not the user closing this note - leave it flagged as open so
                            // it's restored on the next launch instead of falling back to the notes list.
                            if (!_windowManager.IsApplicationExiting)
                            {
                                await _stickyNoteService.SetNoteOpenStateAsync(noteId, false);
                            }

                            await _stickyNoteService.UpdateNoteWindowBoundsAsync(noteId, boundsToPersist.Left, boundsToPersist.Top, boundsToPersist.Width, boundsToPersist.Height);
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

    /// <summary>
    /// Converts the in-memory window state saved earlier in this session into bounds, or null when
    /// this note hasn't been opened yet in this session.
    /// </summary>
    private static Rect? ToBounds(WindowState? savedState) =>
        savedState is null
            ? null
            : new Rect(savedState.Left, savedState.Top, savedState.Width, savedState.Height);

    /// <summary>
    /// Converts the note's persisted position into bounds, falling back to the window's default size
    /// for any dimension that was never saved. Returns null when the note has no saved position at
    /// all, so the caller can centre it instead.
    /// </summary>
    private static Rect? ToBounds(Domain.Models.StickyNote? note, Window window) =>
        note?.WindowLeft is { } left && note.WindowTop is { } top
            ? new Rect(left, top, note.WindowWidth ?? window.Width, note.WindowHeight ?? window.Height)
            : null;

    /// <summary>
    /// True when the window still sits where it was placed, i.e. the user never dragged or resized
    /// it. Uses a sub-pixel tolerance because WPF can round bounds during layout.
    /// </summary>
    private static bool IsUnmovedFrom(Rect closingBounds, Rect? placedBounds) =>
        placedBounds is { } placed &&
        Math.Abs(closingBounds.Left - placed.Left) < 0.5 &&
        Math.Abs(closingBounds.Top - placed.Top) < 0.5 &&
        Math.Abs(closingBounds.Width - placed.Width) < 0.5 &&
        Math.Abs(closingBounds.Height - placed.Height) < 0.5;
}
