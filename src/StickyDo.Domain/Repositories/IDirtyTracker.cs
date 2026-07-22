namespace StickyDo.Domain.Repositories;

/// <summary>
/// Tracks which notes have been modified and need to be persisted.
/// Used to implement selective saving and auto-save functionality.
/// </summary>
public interface IDirtyTracker
{
    /// <summary>
    /// Marks a note as modified (dirty).
    /// </summary>
    void MarkAsDirty(Guid noteId);

    /// <summary>
    /// Marks a note as saved (clean).
    /// </summary>
    void MarkAsClean(Guid noteId);

    /// <summary>
    /// Checks if a note has unsaved changes.
    /// </summary>
    bool IsDirty(Guid noteId);

    /// <summary>
    /// Gets all notes with unsaved changes.
    /// </summary>
    IEnumerable<Guid> GetDirtyNotes();

    /// <summary>
    /// Checks if any note has unsaved changes.
    /// </summary>
    bool HasPendingChanges { get; }

    /// <summary>
    /// Clears all dirty flags.
    /// </summary>
    void ClearAll();
}
