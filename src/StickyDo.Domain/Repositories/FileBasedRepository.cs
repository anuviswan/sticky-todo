using System.Text.Json;
using StickyDo.Domain.Constants;
using StickyDo.Domain.Models;
using StickyDo.Domain.Serialization;
using StickyDo.Domain.Utilities;

namespace StickyDo.Domain.Repositories;

/// <summary>
/// File-based implementation of sticky note repositories.
/// Persists notes as individual JSON files in %LocalAppData%\StickyTODO.
/// Implements both IStickyNoteRepository and IStickyNoteTaskRepository.
/// </summary>
public class FileBasedRepository : IStickyNoteRepository, IStickyNoteTaskRepository
{
    private readonly List<StickyNote> _notes = [];
    private readonly IDirtyTracker _dirtyTracker;
    private bool _initialized;

    public FileBasedRepository()
    {
        _dirtyTracker = new DirtyTracker();
    }

    /// <summary>
    /// Initializes the repository by loading all notes from disk.
    /// Must be called before using the repository.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        try
        {
            System.Diagnostics.Debug.WriteLine("FileBasedRepository: Ensuring data directory exists...");
            PersistencePathHelper.EnsureDataDirectoryExists();
            System.Diagnostics.Debug.WriteLine($"FileBasedRepository: Data directory ready at {PersistencePathHelper.GetDataDirectoryPath()}");

            System.Diagnostics.Debug.WriteLine("FileBasedRepository: Cleaning up orphaned temporary files...");
            AtomicFileWriter.CleanupOrphanedTemporaryFiles();

            System.Diagnostics.Debug.WriteLine("FileBasedRepository: Loading notes from disk...");
            await LoadAllNotesFromDiskAsync();
            System.Diagnostics.Debug.WriteLine($"FileBasedRepository: Loaded {_notes.Count} notes from disk");
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException(
                $"No permission to access the data directory. Ensure you have write access to %LocalAppData%\\StickyTODO. Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FileBasedRepository initialization error: {ex}");
            throw new InvalidOperationException(
                $"Failed to initialize file-based repository: {ex.Message}", ex);
        }

        _initialized = true;
    }

    /// <summary>
    /// Loads all note JSON files from disk into memory.
    /// Skips corrupted files and renames them for manual recovery.
    /// </summary>
    private async Task LoadAllNotesFromDiskAsync()
    {
        _notes.Clear();
        var noteFiles = PersistencePathHelper.GetAllNoteFiles();

        foreach (var filePath in noteFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var note = JsonSerializer.Deserialize<StickyNote>(
                    json,
                    JsonSerializationOptions.Default);

                if (note != null)
                {
                    _notes.Add(note);
                }
            }
            catch (JsonException ex)
            {
                HandleCorruptedFile(filePath, ex);
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"Failed to load note file: {filePath}. Error: {ex.Message}", ex);
            }
        }

        // Don't initialize sample data - start with empty list on first run
        // User can create their own notes from scratch

        // Mark all loaded notes as clean
        _dirtyTracker.ClearAll();
    }

    /// <summary>
    /// Handles corrupted JSON files by renaming them for manual recovery.
    /// </summary>
    private static void HandleCorruptedFile(string filePath, Exception ex)
    {
        var corruptPath = filePath + ".corrupt";
        try
        {
            if (File.Exists(corruptPath))
                File.Delete(corruptPath);

            File.Move(filePath, corruptPath);
        }
        catch
        {
            // Ignore if rename fails
        }
    }

    /// <summary>
    /// Initializes sample data for first-time users.
    /// </summary>
    private async Task InitializeSampleDataAsync()
    {
        var colors = ColorPalette.Colors;
        var titles = new[]
        {
            "Project Meeting Notes",
            "Shopping List",
            "Fix Login Bug",
            "Code Review: PR #456",
            "Learn C# 14 Features",
            "Design System Review",
            "Database Migration Plan",
            "Team Retrospective",
            "API Documentation",
            "Performance Optimization"
        };

        for (int i = 0; i < titles.Length; i++)
        {
            var note = new StickyNote
            {
                Id = Guid.NewGuid(),
                Title = titles[i],
                Status = (StickyNoteStatus)(i % 4),
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                UpdatedAt = DateTime.UtcNow.AddHours(-i),
                ColorArgb = colors[i % colors.Length],
                DisplayOrder = i
            };

            _notes.Add(note);
            await SaveNoteToDiskAsync(note);
        }
    }

    // ========== IStickyNoteRepository Implementation ==========

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

        if (_notes.Any(n => n.Id == note.Id))
            throw new InvalidOperationException($"Note with ID {note.Id} already exists");

        _notes.Add(note);
        _dirtyTracker.MarkAsDirty(note.Id);

        System.Diagnostics.Debug.WriteLine($"FileBasedRepository: Created note '{note.Title}' (ID: {note.Id}), marked dirty. Total notes: {_notes.Count}");

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
            existing.Status = note.Status;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.ColorArgb = note.ColorArgb;
            existing.DisplayOrder = note.DisplayOrder;
            existing.Tasks = note.Tasks;

            _dirtyTracker.MarkAsDirty(note.Id);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        var note = _notes.FirstOrDefault(n => n.Id == id);
        if (note != null)
        {
            _notes.Remove(note);
            _dirtyTracker.MarkAsDirty(id);
            DeleteNoteFromDisk(id);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<StickyNote>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult(_notes.AsEnumerable());

        var lowerQuery = query.ToLower();
        var results = _notes.Where(n =>
            n.Title.ToLower().Contains(lowerQuery)
        );

        return Task.FromResult(results.AsEnumerable());
    }

    public Task<IEnumerable<StickyNote>> GetByStatusAsync(StickyNoteStatus status)
    {
        var results = _notes.Where(n => n.Status == status);
        return Task.FromResult(results.AsEnumerable());
    }

    // ========== IStickyNoteTaskRepository Implementation ==========

    Task<Guid> IStickyNoteTaskRepository.CreateAsync(Guid noteId, StickyNoteTask task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note == null)
            throw new InvalidOperationException($"Note with ID {noteId} not found.");

        note.Tasks.Add(task);
        note.UpdatedAt = DateTime.UtcNow;
        _dirtyTracker.MarkAsDirty(noteId);

        return Task.FromResult(task.Id);
    }

    Task<IEnumerable<StickyNoteTask>> IStickyNoteTaskRepository.GetByNoteIdAsync(Guid noteId)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note == null)
            return Task.FromResult(Enumerable.Empty<StickyNoteTask>());

        return Task.FromResult(note.Tasks.OrderBy(t => t.Order).AsEnumerable());
    }

    Task<StickyNoteTask?> IStickyNoteTaskRepository.GetByIdAsync(Guid taskId)
    {
        var task = _notes
            .SelectMany(n => n.Tasks)
            .FirstOrDefault(t => t.Id == taskId);

        return Task.FromResult(task);
    }

    Task IStickyNoteTaskRepository.UpdateAsync(Guid noteId, StickyNoteTask task)
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
            _dirtyTracker.MarkAsDirty(noteId);
        }

        return Task.CompletedTask;
    }

    Task IStickyNoteTaskRepository.DeleteAsync(Guid noteId, Guid taskId)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note != null)
        {
            var task = note.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                note.Tasks.Remove(task);
                note.UpdatedAt = DateTime.UtcNow;
                _dirtyTracker.MarkAsDirty(noteId);
            }
        }

        return Task.CompletedTask;
    }

    // ========== Persistence Methods ==========

    /// <summary>
    /// Saves a note to disk asynchronously using atomic writes.
    /// </summary>
    public async Task SaveNoteAsync(Guid noteId)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note == null)
        {
            System.Diagnostics.Debug.WriteLine($"FileBasedRepository: Cannot save note {noteId} - not found in memory");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"FileBasedRepository: Saving note '{note.Title}' (ID: {noteId})...");
            await SaveNoteToDiskAsync(note);
            _dirtyTracker.MarkAsClean(noteId);
            System.Diagnostics.Debug.WriteLine($"FileBasedRepository: Successfully saved note '{note.Title}'");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FileBasedRepository: ERROR saving note '{note.Title}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Saves all dirty (modified) notes to disk.
    /// </summary>
    public async Task SaveAllDirtyNotesAsync()
    {
        var dirtyNoteIds = _dirtyTracker.GetDirtyNotes().ToList();
        if (dirtyNoteIds.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("FileBasedRepository: No dirty notes to save");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"FileBasedRepository: Saving {dirtyNoteIds.Count} dirty note(s)...");
        foreach (var noteId in dirtyNoteIds)
        {
            await SaveNoteAsync(noteId);
        }
    }

    /// <summary>
    /// Gets the set of note IDs with unsaved changes.
    /// </summary>
    public IEnumerable<Guid> GetDirtyNotes() => _dirtyTracker.GetDirtyNotes();

    /// <summary>
    /// Checks if any note has unsaved changes.
    /// </summary>
    public bool HasPendingChanges => _dirtyTracker.HasPendingChanges;

    /// <summary>
    /// Clears all persisted notes by deleting all note files from disk.
    /// Useful for resetting the application to a clean state.
    /// </summary>
    public void ClearAllPersistedNotes()
    {
        var noteFiles = PersistencePathHelper.GetAllNoteFiles().ToList();
        foreach (var filePath in noteFiles)
        {
            try
            {
                File.Delete(filePath);
                System.Diagnostics.Debug.WriteLine($"Deleted: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        _notes.Clear();
        _dirtyTracker.ClearAll();
        System.Diagnostics.Debug.WriteLine($"Cleared all {noteFiles.Count} persisted notes");
    }

    // ========== Private Helpers ==========

    private async Task SaveNoteToDiskAsync(StickyNote note)
    {
        var json = JsonSerializer.Serialize(note, JsonSerializationOptions.Default);
        var filePath = PersistencePathHelper.GetNoteFilePath(note.Id);

        await AtomicFileWriter.WriteAtomicAsync(filePath, json);
    }

    private static void DeleteNoteFromDisk(Guid noteId)
    {
        var filePath = PersistencePathHelper.GetNoteFilePath(noteId);
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch
        {
            // Log and continue; file deletion failure is not critical
        }
    }
}
