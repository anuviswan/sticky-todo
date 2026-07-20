namespace StickyDo.Domain.Models;

/// <summary>
/// Represents an individual task within a sticky note.
/// </summary>
public class StickyNoteTask
{
    /// <summary>Unique identifier for the task.</summary>
    public Guid Id { get; set; }

    /// <summary>The task title/description.</summary>
    public required string Title { get; set; }

    /// <summary>Whether the task has been marked as completed.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Display order for sorting tasks within a note.</summary>
    public int Order { get; set; }

    /// <summary>UTC timestamp when the task was created.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the task was last modified.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
