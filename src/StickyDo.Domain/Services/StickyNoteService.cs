namespace StickyDo.Domain.Services;

using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;

/// <summary>
/// Application service for sticky note business logic.
/// </summary>
public class StickyNoteService
{
    private readonly IStickyNoteRepository _repository;

    /// <summary>
    /// Initializes a new instance of the StickyNoteService.
    /// </summary>
    public StickyNoteService(IStickyNoteRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Retrieves all sticky notes.
    /// </summary>
    public async Task<IEnumerable<StickyNote>> GetAllNotesAsync()
    {
        return await _repository.GetAllAsync();
    }

    /// <summary>
    /// Retrieves a specific note by ID.
    /// </summary>
    public async Task<StickyNote?> GetNoteByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(id));

        return await _repository.GetByIdAsync(id);
    }

    /// <summary>
    /// Creates a new sticky note with the provided title and content.
    /// </summary>
    public async Task<Guid> CreateNoteAsync(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Note title cannot be empty.", nameof(title));

        var note = new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Content = content ?? string.Empty,
            Status = StickyNoteStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DisplayOrder = 0
        };

        return await _repository.CreateAsync(note);
    }

    /// <summary>
    /// Updates an existing sticky note.
    /// </summary>
    public async Task UpdateNoteAsync(Guid id, string title, string content, StickyNoteStatus status)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Note title cannot be empty.", nameof(title));

        var note = await _repository.GetByIdAsync(id);
        if (note is null)
            throw new InvalidOperationException($"Note with ID {id} not found.");

        note.Title = title.Trim();
        note.Content = content ?? string.Empty;
        note.Status = status;
        note.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(note);
    }

    /// <summary>
    /// Deletes a sticky note by ID.
    /// </summary>
    public async Task DeleteNoteAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Note ID cannot be empty.", nameof(id));

        await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// Searches for notes by title or content.
    /// </summary>
    public async Task<IEnumerable<StickyNote>> SearchNotesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await _repository.GetAllAsync();

        return await _repository.SearchAsync(query.Trim());
    }

    /// <summary>
    /// Retrieves notes filtered by their status.
    /// </summary>
    public async Task<IEnumerable<StickyNote>> GetNotesByStatusAsync(StickyNoteStatus status)
    {
        return await _repository.GetByStatusAsync(status);
    }
}
