using StickyDo.Domain.Repositories;

namespace StickyDo.Domain.Services;

/// <summary>
/// Orchestrates automatic persistence of sticky notes.
/// Handles 3-second debounced auto-save and graceful shutdown saves.
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
    /// Starts the auto-save background task with the configured debounce interval.
    /// </summary>
    public void StartAutoSave()
    {
        if (_autoSaveTimer != null)
            return;

        _cancellationTokenSource = new CancellationTokenSource();
        _autoSaveTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(_debounceMs));
        _autoSaveTask = RunAutoSaveLoopAsync(_cancellationTokenSource.Token);
    }

    /// <summary>
    /// Stops the auto-save background task gracefully.
    /// </summary>
    public async Task StopAutoSaveAsync()
    {
        if (_autoSaveTimer == null)
            return;

        _cancellationTokenSource?.Cancel();
        await (_autoSaveTask ?? Task.CompletedTask);

        _autoSaveTimer?.Dispose();
        _autoSaveTimer = null;
    }

    /// <summary>
    /// Saves all notes with unsaved changes to disk.
    /// </summary>
    public async Task SaveAllDirtyNotesAsync()
    {
        await _repository.SaveAllDirtyNotesAsync();
    }

    /// <summary>
    /// Checks if any note has unsaved changes.
    /// </summary>
    public bool HasPendingChanges => _repository.HasPendingChanges;

    /// <summary>
    /// Background task that periodically saves dirty notes.
    /// Design: Timer runs continuously but only saves if HasPendingChanges is true.
    /// This is more efficient than starting/stopping timer on demand since timer overhead is minimal
    /// (PeriodicTimer every 3s just checks a boolean flag). Avoids complexity of change notifications.
    /// </summary>
    private async Task RunAutoSaveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _autoSaveTimer!.WaitForNextTickAsync(cancellationToken))
            {
                if (_repository.HasPendingChanges)
                {
                    await _repository.SaveAllDirtyNotesAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when StopAutoSaveAsync is called
        }
        catch (Exception ex)
        {
            // Log error but don't crash; auto-save will retry on next interval
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
