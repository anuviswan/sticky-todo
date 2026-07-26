using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;

namespace StickyDo.Domain.Services;

/// <summary>
/// Orchestrates automatic persistence of sticky notes.
/// Auto-save timer only runs when the user is actively editing.
/// </summary>
public class PersistenceService : IAsyncDisposable
{
    private readonly FileBasedRepository _repository;
    private PeriodicTimer? _autoSaveTimer;
    private Task? _autoSaveTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly int _debounceMs;

    public PersistenceService(FileBasedRepository repository, int debounceMs = 3000)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _debounceMs = debounceMs;
    }

    /// <summary>
    /// Raised whenever a specific note transitions between Saving/Saved/NotSaved, so that
    /// open note windows can reflect their live save status without polling.
    /// </summary>
    public event EventHandler<NoteSaveStateChangedEventArgs>? NoteSaveStateChanged;

    /// <summary>
    /// Starts the auto-save background task when user begins editing.
    /// If already running, does nothing.
    /// </summary>
    public void StartAutoSave()
    {
        if (_autoSaveTimer != null)
            return;

        _cancellationTokenSource = new CancellationTokenSource();
        _autoSaveTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(_debounceMs));
        _autoSaveTask = RunAutoSaveLoopAsync(_cancellationTokenSource.Token);
        System.Diagnostics.Debug.WriteLine("Auto-save timer started - user is editing");
    }

    /// <summary>
    /// Stops the auto-save background task when user finishes editing.
    /// </summary>
    public async Task StopAutoSaveAsync()
    {
        if (_autoSaveTimer == null)
            return;

        System.Diagnostics.Debug.WriteLine("Auto-save timer stopped - user finished editing");
        _cancellationTokenSource?.Cancel();
        await (_autoSaveTask ?? Task.CompletedTask);

        _autoSaveTimer?.Dispose();
        _autoSaveTimer = null;
    }

    /// <summary>
    /// Saves all notes with unsaved changes to disk immediately, raising
    /// <see cref="NoteSaveStateChanged"/> for each one so subscribers see live progress.
    /// A note that fails to save is left dirty (retried on the next tick) and does not
    /// prevent the remaining dirty notes from being saved.
    /// </summary>
    public async Task SaveAllDirtyNotesAsync()
    {
        var dirtyNoteIds = _repository.GetDirtyNotes().ToList();
        foreach (var noteId in dirtyNoteIds)
        {
            NoteSaveStateChanged?.Invoke(this, new NoteSaveStateChangedEventArgs(noteId, NoteSaveState.Saving));

            try
            {
                await _repository.SaveNoteAsync(noteId);
                NoteSaveStateChanged?.Invoke(this, new NoteSaveStateChangedEventArgs(noteId, NoteSaveState.Saved));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-save error for note {noteId}: {ex}");
                NoteSaveStateChanged?.Invoke(this, new NoteSaveStateChangedEventArgs(noteId, NoteSaveState.NotSaved));
            }
        }
    }

    /// <summary>
    /// Checks if any note has unsaved changes.
    /// </summary>
    public bool HasPendingChanges => _repository.HasPendingChanges;

    /// <summary>
    /// Background task that periodically saves dirty notes while user is editing.
    /// Only runs when StartAutoSave() is called; stops when StopAutoSaveAsync() is called.
    /// </summary>
    private async Task RunAutoSaveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _autoSaveTimer!.WaitForNextTickAsync(cancellationToken))
            {
                if (_repository.HasPendingChanges)
                {
                    await SaveAllDirtyNotesAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when StopAutoSaveAsync is called
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Auto-save error: {ex}");
        }
    }

    /// <summary>
    /// Disposes resources and stops auto-save.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await StopAutoSaveAsync();
        _cancellationTokenSource?.Dispose();
    }
}
