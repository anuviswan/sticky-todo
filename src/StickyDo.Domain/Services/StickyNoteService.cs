namespace StickyDo.Domain.Services;

using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;

/// <summary>
/// Application service for sticky note business logic.
/// </summary>
public class StickyNoteService
{
    private readonly IStickyNoteRepository _noteRepository;
    private readonly IStickyNoteTaskRepository _taskRepository;

    /// <summary>
    /// Initializes a new instance of the StickyNoteService.
    /// </summary>
    public StickyNoteService(IStickyNoteRepository noteRepository, IStickyNoteTaskRepository taskRepository)
    {
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    }

    /// <summary>
    /// Retrieves all sticky notes.
    /// </summary>
    public async Task<IEnumerable<StickyNote>> GetAllNotesAsync()
    {
        return await _noteRepository.GetAllAsync();
    }

    /// <summary>
    /// Retrieves a specific note by ID.
    /// </summary>
    public async Task<StickyNote?> GetNoteByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(id));

        return await _noteRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Creates a new sticky note with the provided title.
    /// </summary>
    public async Task<Guid> CreateNoteAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Note title cannot be empty.", nameof(title));

        var note = new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Status = StickyNoteStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DisplayOrder = 0
        };

        return await _noteRepository.CreateAsync(note);
    }

    /// <summary>
    /// Updates an existing sticky note.
    /// </summary>
    public async Task UpdateNoteAsync(Guid id, string title, StickyNoteStatus status)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Note title cannot be empty.", nameof(title));

        var note = await _noteRepository.GetByIdAsync(id);
        if (note is null)
            throw new InvalidOperationException($"Note with ID {id} not found.");

        note.Title = title.Trim();
        note.Status = status;
        note.UpdatedAt = DateTime.UtcNow;

        await _noteRepository.UpdateAsync(note);
    }

    /// <summary>
    /// Deletes a sticky note by ID.
    /// </summary>
    public async Task DeleteNoteAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(id));

        await _noteRepository.DeleteAsync(id);
    }

    /// <summary>
    /// Searches for notes by title or content.
    /// </summary>
    public async Task<IEnumerable<StickyNote>> SearchNotesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await _noteRepository.GetAllAsync();

        return await _noteRepository.SearchAsync(query.Trim());
    }

    /// <summary>
    /// Retrieves notes filtered by their status.
    /// </summary>
    public async Task<IEnumerable<StickyNote>> GetNotesByStatusAsync(StickyNoteStatus status)
    {
        return await _noteRepository.GetByStatusAsync(status);
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

    /// <summary>
    /// Gets the next note number for auto-generated note titles (e.g., "Note 1", "Note 2").
    /// </summary>
    public async Task<int> GetNextNoteNumberAsync()
    {
        var allNotes = await _noteRepository.GetAllAsync();
        var noteNumbers = allNotes
            .Where(n => n.Title.StartsWith("Note ") && int.TryParse(n.Title.Substring(5), out _))
            .Select(n => int.Parse(n.Title.Substring(5)))
            .ToList();

        return noteNumbers.Any() ? noteNumbers.Max() + 1 : 1;
    }
}
