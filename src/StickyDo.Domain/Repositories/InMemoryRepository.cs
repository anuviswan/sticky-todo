using StickyDo.Domain.Models;

namespace StickyDo.Domain.Repositories;

/// <summary>
/// In-memory implementation of sticky note repository for Phase 1 testing.
/// Will be replaced with SQLite implementation in Phase 2.
/// </summary>
public class InMemoryRepository : IStickyNoteRepository
{
    private readonly List<StickyNote> _notes = new();

    public InMemoryRepository()
    {
        InitializeSampleData();
    }

    public Task<IEnumerable<StickyNote>> GetAllAsync()
    {
        return Task.FromResult(_notes.AsEnumerable());
    }

    public Task<StickyNote?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(_notes.FirstOrDefault(n => n.Id == id));
    }

    public Task<Guid> CreateAsync(StickyNote note)
    {
        if (note == null)
            throw new ArgumentNullException(nameof(note));

        _notes.Add(note);
        return Task.FromResult(note.Id);
    }

    public Task UpdateAsync(StickyNote note)
    {
        if (note == null)
            throw new ArgumentNullException(nameof(note));

        var existing = _notes.FirstOrDefault(n => n.Id == note.Id);
        if (existing != null)
        {
            existing.Title = note.Title;
            existing.Content = note.Content;
            existing.Status = note.Status;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.ColorArgb = note.ColorArgb;
            existing.DisplayOrder = note.DisplayOrder;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        var note = _notes.FirstOrDefault(n => n.Id == id);
        if (note != null)
            _notes.Remove(note);

        return Task.CompletedTask;
    }

    public Task<IEnumerable<StickyNote>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult(_notes.AsEnumerable());

        var lowerQuery = query.ToLower();
        var results = _notes.Where(n =>
            n.Title.ToLower().Contains(lowerQuery) ||
            n.Content.ToLower().Contains(lowerQuery)
        );

        return Task.FromResult(results.AsEnumerable());
    }

    public Task<IEnumerable<StickyNote>> GetByStatusAsync(StickyNoteStatus status)
    {
        var results = _notes.Where(n => n.Status == status);
        return Task.FromResult(results.AsEnumerable());
    }

    private void InitializeSampleData()
    {
        _notes.Add(new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = "Project Meeting Notes",
            Content = "Discuss Q3 roadmap and team assignments\n- Review current backlog\n- Plan sprint goals",
            Status = StickyNoteStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-2),
            ColorArgb = 0xFFFFCC07, // Yellow
            DisplayOrder = 0
        });

        _notes.Add(new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = "Shopping List",
            Content = "Milk\nEggs\nBread\nCoffee\nButter",
            Status = StickyNoteStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-24),
            ColorArgb = 0xFF00B36F, // Green
            DisplayOrder = 1
        });

        _notes.Add(new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = "Fix Login Bug",
            Content = "Session timeout issue on mobile devices\n- Investigate session management\n- Check authentication flow\n- Test on multiple browsers",
            Status = StickyNoteStatus.Urgent,
            CreatedAt = DateTime.UtcNow.AddHours(-6),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            ColorArgb = 0xFFE53935, // Red
            DisplayOrder = 2
        });

        _notes.Add(new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = "Code Review: PR #456",
            Content = "Review requested for authentication refactor\n- Check error handling\n- Verify test coverage\n- Ensure backward compatibility",
            Status = StickyNoteStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30),
            ColorArgb = 0xFF0066CC, // Blue
            DisplayOrder = 3
        });

        _notes.Add(new StickyNote
        {
            Id = Guid.NewGuid(),
            Title = "Learn C# 14 Features",
            Content = "New features to explore:\n- Primary constructors\n- Collection initializers\n- LINQ improvements",
            Status = StickyNoteStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow.AddDays(-4),
            ColorArgb = 0xFFC2185B, // Pink
            DisplayOrder = 4
        });
    }
}
