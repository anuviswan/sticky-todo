namespace StickyDo.Domain.Services;

using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;

/// <summary>
/// Application service for sticky note task business logic.
/// </summary>
public class StickyNoteTaskService
{
    private readonly IStickyNoteRepository _noteRepository;
    private readonly IStickyNoteTaskRepository _taskRepository;

    /// <summary>
    /// Initializes a new instance of the StickyNoteTaskService.
    /// </summary>
    public StickyNoteTaskService(IStickyNoteRepository noteRepository, IStickyNoteTaskRepository taskRepository)
    {
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    }

    /// <summary>
    /// Creates a new task within a note.
    /// </summary>
    public async Task<Guid> CreateTaskAsync(Guid noteId, string title)
    {
        if (noteId == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(noteId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title cannot be empty.", nameof(title));

        var note = await _noteRepository.GetByIdAsync(noteId);
        if (note is null)
            throw new InvalidOperationException($"Note with ID {noteId} not found.");

        var task = new StickyNoteTask
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            IsCompleted = false,
            Order = note.Tasks.Count,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _taskRepository.CreateAsync(noteId, task);
    }

    /// <summary>
    /// Retrieves all tasks for a specific note.
    /// </summary>
    public async Task<IEnumerable<StickyNoteTask>> GetTasksByNoteIdAsync(Guid noteId)
    {
        if (noteId == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(noteId));

        return await _taskRepository.GetByNoteIdAsync(noteId);
    }

    /// <summary>
    /// Updates an existing task's completion status or title.
    /// </summary>
    public async Task UpdateTaskAsync(Guid noteId, Guid taskId, string title, bool isCompleted)
    {
        if (noteId == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(noteId));

        if (taskId == Guid.Empty)
            throw new ArgumentException("Task ID cannot be empty.", nameof(taskId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title cannot be empty.", nameof(title));

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task is null)
            throw new InvalidOperationException($"Task with ID {taskId} not found.");

        task.Title = title.Trim();
        task.IsCompleted = isCompleted;
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.UpdateAsync(noteId, task);
    }

    /// <summary>
    /// Deletes a task from a note.
    /// </summary>
    public async Task DeleteTaskAsync(Guid noteId, Guid taskId)
    {
        if (noteId == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(noteId));

        if (taskId == Guid.Empty)
            throw new ArgumentException("Task ID cannot be empty.", nameof(taskId));

        await _taskRepository.DeleteAsync(noteId, taskId);
    }
}
