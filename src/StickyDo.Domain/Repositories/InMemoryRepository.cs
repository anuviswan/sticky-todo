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

    public Task<Guid> CreateTaskAsync(Guid noteId, StickyNoteTask task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note == null)
            throw new InvalidOperationException($"Note with ID {noteId} not found.");

        note.Tasks.Add(task);
        note.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult(task.Id);
    }

    public Task<IEnumerable<StickyNoteTask>> GetTasksByNoteIdAsync(Guid noteId)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note == null)
            return Task.FromResult(Enumerable.Empty<StickyNoteTask>());

        return Task.FromResult(note.Tasks.OrderBy(t => t.Order).AsEnumerable());
    }

    public Task<StickyNoteTask?> GetTaskByIdAsync(Guid taskId)
    {
        var task = _notes
            .SelectMany(n => n.Tasks)
            .FirstOrDefault(t => t.Id == taskId);

        return Task.FromResult(task);
    }

    public Task UpdateTaskAsync(Guid noteId, StickyNoteTask task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note == null)
            throw new InvalidOperationException($"Note with ID {noteId} not found.");

        var existingTask = note.Tasks.FirstOrDefault(t => t.Id == task.Id);
        if (existingTask != null)
        {
            existingTask.Title = task.Title;
            existingTask.IsCompleted = task.IsCompleted;
            existingTask.Order = task.Order;
            existingTask.UpdatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task DeleteTaskAsync(Guid noteId, Guid taskId)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note != null)
        {
            var task = note.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                note.Tasks.Remove(task);
                note.UpdatedAt = DateTime.UtcNow;
            }
        }

        return Task.CompletedTask;
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
