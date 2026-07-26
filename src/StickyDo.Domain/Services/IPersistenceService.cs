namespace StickyDo.Domain.Services;

/// <summary>
/// Orchestrates automatic persistence of sticky notes.
/// </summary>
public interface IPersistenceService
{
    /// <summary>
    /// Raised whenever a specific note transitions between Saving/Saved/NotSaved, so that
    /// open note windows can reflect their live save status without polling.
    /// </summary>
    event EventHandler<NoteSaveStateChangedEventArgs>? NoteSaveStateChanged;

    /// <summary>
    /// Starts the auto-save background task when user begins editing.
    /// If already running, does nothing.
    /// </summary>
    void StartAutoSave();

    /// <summary>
    /// Stops the auto-save background task when user finishes editing.
    /// </summary>
    Task StopAutoSaveAsync();

    /// <summary>
    /// Saves all notes with unsaved changes to disk immediately, raising
    /// <see cref="NoteSaveStateChanged"/> for each one so subscribers see live progress.
    /// </summary>
    Task SaveAllDirtyNotesAsync();

    /// <summary>
    /// Checks if any note has unsaved changes.
    /// </summary>
    bool HasPendingChanges { get; }
}
