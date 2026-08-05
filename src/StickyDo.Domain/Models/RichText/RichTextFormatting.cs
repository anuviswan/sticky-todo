namespace StickyDo.Domain.Models.RichText;

/// <summary>
/// Versioned document describing the formatting applied to a plain-text field
/// (e.g. <see cref="StickyNote.Content"/> or <see cref="StickyNoteTask.Title"/>).
/// The plain text itself remains the single source of truth; this document only
/// layers style spans on top of it.
/// </summary>
public sealed class RichTextFormatting
{
    /// <summary>Current schema version produced by this build.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Schema version this document was written with.</summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>Formatting spans, in no particular order, over the associated plain text.</summary>
    public List<RichTextSpan> Spans { get; set; } = [];
}
