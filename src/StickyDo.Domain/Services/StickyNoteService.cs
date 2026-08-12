namespace StickyDo.Domain.Services;

using StickyDo.Domain.Models;
using StickyDo.Domain.Models.RichText;
using StickyDo.Domain.Repositories;

/// <summary>
/// Application service for sticky note business logic.
/// </summary>
public class StickyNoteService
{
    /// <summary>
    /// Title of the onboarding task seeded into a user's very first note.
    /// </summary>
    private const string DemoTaskTitle = "First Task";

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
    /// Creates a new sticky note with the provided title, optional color, and type (defaults to Todo).
    /// A Todo note is seeded with a "First Task" demo task only when it is the user's very first
    /// note (i.e. no other notes currently exist) - later notes start empty.
    /// </summary>
    public async Task<Guid> CreateNoteAsync(string title, uint? colorArgb = null, NoteType type = NoteType.Todo)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Note title cannot be empty.", nameof(title));

        var isFirstNote = !(await _noteRepository.GetAllAsync()).Any();

        var note = new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Status = StickyNoteStatus.Active,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DisplayOrder = 0,
            ColorArgb = colorArgb
        };

        var noteId = await _noteRepository.CreateAsync(note);

        if (isFirstNote && type == NoteType.Todo)
        {
            var demoTask = new StickyNoteTask
            {
                Id = Guid.NewGuid(),
                Title = DemoTaskTitle,
                IsCompleted = false,
                Order = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _taskRepository.CreateAsync(noteId, demoTask);
        }

        return noteId;
    }

    /// <summary>
    /// Updates an existing sticky note. <paramref name="content"/> is only applied when
    /// non-null, so callers that don't touch the free-form body (e.g. a color change) don't
    /// accidentally clear it. <paramref name="contentFormatting"/> always travels together
    /// with <paramref name="content"/> - it comes from the same editing surface and is applied
    /// (including clearing it to null) only when content is also being updated.
    /// </summary>
    public async Task UpdateNoteAsync(Guid id, string title, StickyNoteStatus status, uint? color = null, string? content = null, RichTextFormatting? contentFormatting = null)
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

        if (color.HasValue)
            note.ColorArgb = color.Value;

        if (content is not null)
        {
            note.Content = content;
            note.ContentFormatting = contentFormatting;
        }

        await _noteRepository.UpdateAsync(note);
    }

    /// <summary>
    /// Sets whether a note is currently open as a floating sticky note window.
    /// </summary>
    public async Task SetNoteOpenStateAsync(Guid id, bool isOpened)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(id));

        var note = await _noteRepository.GetByIdAsync(id);
        if (note is null)
            throw new InvalidOperationException($"Note with ID {id} not found.");

        note.IsOpened = isOpened;

        await _noteRepository.UpdateAsync(note);
    }

    /// <summary>
    /// Sets whether a note is pinned, preventing it from being moved or closed.
    /// </summary>
    public async Task SetNotePinnedAsync(Guid id, bool isPinned)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(id));

        var note = await _noteRepository.GetByIdAsync(id);
        if (note is null)
            throw new InvalidOperationException($"Note with ID {id} not found.");

        note.IsPinned = isPinned;

        await _noteRepository.UpdateAsync(note);
    }

    /// <summary>
    /// Sets whether a note is marked as a favourite.
    /// </summary>
    public async Task SetNoteFavoriteAsync(Guid id, bool isFavorite)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(id));

        var note = await _noteRepository.GetByIdAsync(id);
        if (note is null)
            throw new InvalidOperationException($"Note with ID {id} not found.");

        note.IsFavorite = isFavorite;

        await _noteRepository.UpdateAsync(note);
    }

    /// <summary>
    /// Sets whether a note is a structured Todo or a free-form Note.
    /// </summary>
    public async Task SetNoteTypeAsync(Guid id, NoteType type)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(id));

        var note = await _noteRepository.GetByIdAsync(id);
        if (note is null)
            throw new InvalidOperationException($"Note with ID {id} not found.");

        note.Type = type;

        await _noteRepository.UpdateAsync(note);
    }

    /// <summary>
    /// Persists the floating window's current screen position and size for a note, so it can be
    /// restored to the same spot the next time it's opened, including after an application restart.
    /// </summary>
    public async Task UpdateNoteWindowBoundsAsync(Guid id, double left, double top, double width, double height)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(id));

        var note = await _noteRepository.GetByIdAsync(id);
        if (note is null)
            throw new InvalidOperationException($"Note with ID {id} not found.");

        note.WindowLeft = left;
        note.WindowTop = top;
        note.WindowWidth = width;
        note.WindowHeight = height;

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
    /// Retrieves notes filtered by their status.
    /// </summary>
    public async Task<IEnumerable<StickyNote>> GetNotesByStatusAsync(StickyNoteStatus status)
    {
        return await _noteRepository.GetByStatusAsync(status);
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
