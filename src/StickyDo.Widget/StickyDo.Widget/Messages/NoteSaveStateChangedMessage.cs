using StickyDo.Domain.Models;

namespace StickyDo.Widget.Messages;

/// <summary>
/// Broadcast whenever a note's save state changes (Saving/Saved/NotSaved), so the open
/// sticky note window for that note can update its footer status live.
/// </summary>
public sealed record NoteSaveStateChangedMessage(Guid NoteId, NoteSaveState State);
