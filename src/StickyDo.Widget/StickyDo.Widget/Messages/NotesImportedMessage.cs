namespace StickyDo.Widget.Messages;

/// <summary>
/// Broadcast after notes have been imported from a backup file, so the notes list can reload
/// from the repository without restarting the app.
/// </summary>
public sealed record NotesImportedMessage(int ImportedCount);
