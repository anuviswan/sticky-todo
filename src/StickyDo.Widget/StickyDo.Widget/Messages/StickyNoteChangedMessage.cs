namespace StickyDo.Widget.Messages;

/// <summary>
/// Describes the kind of change that happened to a sticky note.
/// </summary>
public enum StickyNoteChangeType
{
    Created,
    Updated,
    Deleted
}

/// <summary>
/// Broadcast whenever a sticky note is created, updated, or deleted, so that
/// other view models (e.g. the notes list) can refresh without restarting the app.
/// </summary>
public sealed record StickyNoteChangedMessage(Guid NoteId, StickyNoteChangeType ChangeType);
