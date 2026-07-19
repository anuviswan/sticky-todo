namespace StickyDo.Domain.Repositories;

using StickyDo.Domain.Models;

/// <summary>
/// Repository interface for sticky note data access.
/// </summary>
public interface IStickyNoteRepository
{
    /// <summary>
    /// Retrieves all sticky notes from the data store.
    /// </summary>
    Task<IEnumerable<StickyNote>> GetAllAsync();

    /// <summary>
    /// Retrieves a specific sticky note by ID.
    /// </summary>
    Task<StickyNote?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new sticky note and returns its ID.
    /// </summary>
    Task<Guid> CreateAsync(StickyNote note);

    /// <summary>
    /// Updates an existing sticky note.
    /// </summary>
    Task UpdateAsync(StickyNote note);

    /// <summary>
    /// Deletes a sticky note by ID.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Searches for sticky notes by title or content.
    /// </summary>
    Task<IEnumerable<StickyNote>> SearchAsync(string query);

    /// <summary>
    /// Retrieves sticky notes filtered by status.
    /// </summary>
    Task<IEnumerable<StickyNote>> GetByStatusAsync(StickyNoteStatus status);

    /// <summary>
    /// Creates a new task within a note.
    /// </summary>
    Task<Guid> CreateTaskAsync(Guid noteId, StickyNoteTask task);

    /// <summary>
    /// Retrieves all tasks for a specific note.
    /// </summary>
    Task<IEnumerable<StickyNoteTask>> GetTasksByNoteIdAsync(Guid noteId);

    /// <summary>
    /// Retrieves a specific task by ID.
    /// </summary>
    Task<StickyNoteTask?> GetTaskByIdAsync(Guid taskId);

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    Task UpdateTaskAsync(Guid noteId, StickyNoteTask task);

    /// <summary>
    /// Deletes a task by ID.
    /// </summary>
    Task DeleteTaskAsync(Guid noteId, Guid taskId);
}
