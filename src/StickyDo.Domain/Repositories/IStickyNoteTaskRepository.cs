namespace StickyDo.Domain.Repositories;

using StickyDo.Domain.Models;

/// <summary>
/// Repository interface for sticky note task data access.
/// Handles CRUD operations for tasks within notes.
/// </summary>
public interface IStickyNoteTaskRepository
{
    /// <summary>
    /// Creates a new task within a note.
    /// </summary>
    Task<Guid> CreateAsync(Guid noteId, StickyNoteTask task);

    /// <summary>
    /// Retrieves all tasks for a specific note.
    /// </summary>
    Task<IEnumerable<StickyNoteTask>> GetByNoteIdAsync(Guid noteId);

    /// <summary>
    /// Retrieves a specific task by ID.
    /// </summary>
    Task<StickyNoteTask?> GetByIdAsync(Guid taskId);

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    Task UpdateAsync(Guid noteId, StickyNoteTask task);

    /// <summary>
    /// Deletes a task by ID.
    /// </summary>
    Task DeleteAsync(Guid noteId, Guid taskId);
}
