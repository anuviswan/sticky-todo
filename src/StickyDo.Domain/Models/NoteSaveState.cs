namespace StickyDo.Domain.Models;

/// <summary>
/// Represents whether a sticky note's latest changes have been persisted to disk.
/// </summary>
public enum NoteSaveState
{
    /// <summary>All changes have been successfully persisted.</summary>
    Saved,

    /// <summary>A save operation is currently in progress.</summary>
    Saving,

    /// <summary>The note contains changes that have not yet been saved.</summary>
    NotSaved
}
