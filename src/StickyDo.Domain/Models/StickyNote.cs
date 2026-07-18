namespace StickyDo.Domain.Models;

/// <summary>
/// Represents a sticky note/todo item in the application.
/// </summary>
public class StickyNote
{
    /// <summary>Unique identifier for the note.</summary>
    public Guid Id { get; set; }

    /// <summary>Title or heading of the note.</summary>
    public required string Title { get; set; }

    /// <summary>Main content/body of the note.</summary>
    public required string Content { get; set; }

    /// <summary>Current status of the note.</summary>
    public StickyNoteStatus Status { get; set; } = StickyNoteStatus.Active;

    /// <summary>UTC timestamp when the note was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the note was last modified.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>ARGB color value for UI display.</summary>
    public uint? ColorArgb { get; set; }

    /// <summary>Display order for sorting in the list.</summary>
    public int DisplayOrder { get; set; }
}
