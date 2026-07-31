namespace StickyDo.Domain.Models;

/// <summary>
/// Represents whether a sticky note is a structured Todo or a free-form Note.
/// </summary>
public enum NoteType
{
    /// <summary>A structured task list with checkable items.</summary>
    Todo,

    /// <summary>A free-form note without a task list.</summary>
    Note
}
