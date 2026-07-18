namespace StickyDo.Domain.Models;

/// <summary>
/// Represents the status/priority of a sticky note.
/// </summary>
public enum StickyNoteStatus
{
    /// <summary>Note is active and visible.</summary>
    Active,

    /// <summary>Note has been marked as completed.</summary>
    Completed,

    /// <summary>Note has been archived (hidden but not deleted).</summary>
    Archived,

    /// <summary>Note requires immediate attention.</summary>
    Urgent
}
