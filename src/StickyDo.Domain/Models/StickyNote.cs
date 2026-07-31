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

    /// <summary>List of tasks in this note. Used for notes with structured task lists.</summary>
    public List<StickyNoteTask> Tasks { get; set; } = [];

    /// <summary>Current status of the note.</summary>
    public StickyNoteStatus Status { get; set; } = StickyNoteStatus.Active;

    /// <summary>Whether this item is a structured Todo or a free-form Note.</summary>
    public NoteType Type { get; set; } = NoteType.Todo;

    /// <summary>UTC timestamp when the note was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the note was last modified.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>ARGB color value for UI display.</summary>
    public uint? ColorArgb { get; set; }

    /// <summary>Display order for sorting in the list.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether the note is currently open as a floating sticky note window.</summary>
    public bool IsOpened { get; set; }

    /// <summary>Whether the note is pinned, preventing it from being moved or closed.</summary>
    public bool IsPinned { get; set; }

    /// <summary>Whether the note is marked as a favourite, surfacing it in the Favourites view.</summary>
    public bool IsFavorite { get; set; }

    /// <summary>Persisted screen position (left) of the floating note window, from the last time it was closed.</summary>
    public double? WindowLeft { get; set; }

    /// <summary>Persisted screen position (top) of the floating note window, from the last time it was closed.</summary>
    public double? WindowTop { get; set; }

    /// <summary>Persisted width of the floating note window, from the last time it was closed.</summary>
    public double? WindowWidth { get; set; }

    /// <summary>Persisted height of the floating note window, from the last time it was closed.</summary>
    public double? WindowHeight { get; set; }
}
