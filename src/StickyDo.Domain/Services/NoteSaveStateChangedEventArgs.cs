using StickyDo.Domain.Models;

namespace StickyDo.Domain.Services;

/// <summary>
/// Raised by <see cref="PersistenceService"/> whenever a specific note's save state changes.
/// </summary>
public sealed class NoteSaveStateChangedEventArgs(Guid noteId, NoteSaveState state) : EventArgs
{
    public Guid NoteId { get; } = noteId;

    public NoteSaveState State { get; } = state;
}
