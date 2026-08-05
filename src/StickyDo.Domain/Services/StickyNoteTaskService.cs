namespace StickyDo.Domain.Services;

using StickyDo.Domain.Models;
using StickyDo.Domain.Models.RichText;
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
    /// Updates an existing task's completion status or title. <paramref name="titleFormatting"/>
    /// travels together with <paramref name="title"/> - both come from the same editing surface.
    /// </summary>
    public async Task UpdateTaskAsync(Guid noteId, Guid taskId, string title, bool isCompleted, RichTextFormatting? titleFormatting = null)
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
        task.TitleFormatting = titleFormatting;
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
    /// Converts a note between the structured Todo and free-form Note representations.
    /// Todo -> Note joins each task's title into a line of free-form text (dropping the
    /// checkbox structure); Note -> Todo splits the text into lines and creates one unchecked
    /// task per non-empty line. Only the representation matching the new type is kept - the
    /// other one is cleared, since a note only ever displays one of them at a time. Rich-text
    /// formatting is dropped in both directions, since per-task formatting can't be re-expressed
    /// as offsets into joined text (and vice versa). Identity and other metadata (Id, color,
    /// favourite, pinned, etc.) are untouched.
    /// </summary>
    public async Task<StickyNote> ConvertNoteTypeAsync(Guid noteId, NoteType targetType)
    {
        if (noteId == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(noteId));

        var note = await _noteRepository.GetByIdAsync(noteId);
        if (note is null)
            throw new InvalidOperationException($"Note with ID {noteId} not found.");

        if (note.Type != targetType)
        {
            if (targetType == NoteType.Note)
            {
                var lines = note.Tasks.OrderBy(t => t.Order).Select(t => t.Title);
                note.Content = string.Join(Environment.NewLine, lines);
                // Per-task formatting can't be meaningfully re-expressed as offsets into the
                // joined text, so it's intentionally dropped rather than carried over.
                note.ContentFormatting = null;

                foreach (var task in note.Tasks.OrderBy(t => t.Order).ToList())
                    await _taskRepository.DeleteAsync(noteId, task.Id);
            }
            else
            {
                var lines = (note.Content ?? string.Empty).Split('\n');
                var order = 0;

                foreach (var rawLine in lines)
                {
                    var line = rawLine.TrimEnd('\r').Trim();
                    if (line.Length == 0)
                        continue;

                    var task = new StickyNoteTask
                    {
                        Id = Guid.NewGuid(),
                        Title = line,
                        IsCompleted = false,
                        Order = order++,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _taskRepository.CreateAsync(noteId, task);
                }

                // The note's own formatting doesn't map onto any single new task, so it's
                // intentionally dropped rather than carried over (new tasks are created plain).
                note.Content = null;
                note.ContentFormatting = null;
            }

            note.Type = targetType;
            await _noteRepository.UpdateAsync(note);
        }

        return (await _noteRepository.GetByIdAsync(noteId))!;
    }
}
