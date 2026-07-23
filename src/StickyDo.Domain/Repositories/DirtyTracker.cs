using System.Collections.Concurrent;

namespace StickyDo.Domain.Repositories;

/// <summary>
/// Thread-safe implementation of IDirtyTracker using ConcurrentDictionary.
/// Tracks which notes have unsaved changes for selective persistence.
/// </summary>
public class DirtyTracker : IDirtyTracker
{
    private readonly ConcurrentDictionary<Guid, bool> _dirtyNotes = new();

    public void MarkAsDirty(Guid noteId)
    {
        _dirtyNotes[noteId] = true;
    }

    public void MarkAsClean(Guid noteId)
    {
        _dirtyNotes.TryRemove(noteId, out _);
    }

    public bool IsDirty(Guid noteId)
    {
        return _dirtyNotes.ContainsKey(noteId);
    }

    public IEnumerable<Guid> GetDirtyNotes()
    {
        return _dirtyNotes.Keys.ToList();
    }

    public bool HasPendingChanges => !_dirtyNotes.IsEmpty;

    public void ClearAll()
    {
        _dirtyNotes.Clear();
    }
}
