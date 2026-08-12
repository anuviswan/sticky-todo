namespace StickyDo.Domain.Repositories;

using StickyDo.Domain.Models;

/// <summary>
/// Repository interface for sticky note data access.
/// Handles CRUD and query operations for sticky notes.
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
    /// Updates an existing sticky note. Persists <paramref name="note"/>'s <c>UpdatedAt</c>
    /// as given rather than stamping it with the current time - callers decide whether an
    /// operation represents a content change (and should bump it) or a metadata/organizational
    /// change like favouriting or pinning (which should leave it untouched).
    /// </summary>
    Task UpdateAsync(StickyNote note);

    /// <summary>
    /// Deletes a sticky note by ID.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Retrieves sticky notes filtered by status.
    /// </summary>
    Task<IEnumerable<StickyNote>> GetByStatusAsync(StickyNoteStatus status);
}
