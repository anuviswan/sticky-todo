using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Repositories;

namespace StickyDo.Domain.Tests.Repositories;

[TestClass]
public class DirtyTrackerTests
{
    [TestMethod]
    public void MarkAsDirty_AddsNoteToTracking()
    {
        // Arrange
        var tracker = new DirtyTracker();
        var noteId = Guid.NewGuid();

        // Act
        tracker.MarkAsDirty(noteId);

        // Assert
        Assert.IsTrue(tracker.IsDirty(noteId));
    }

    [TestMethod]
    public void MarkAsClean_RemovesNoteFromTracking()
    {
        // Arrange
        var tracker = new DirtyTracker();
        var noteId = Guid.NewGuid();
        tracker.MarkAsDirty(noteId);

        // Act
        tracker.MarkAsClean(noteId);

        // Assert
        Assert.IsFalse(tracker.IsDirty(noteId));
    }

    [TestMethod]
    public void GetDirtyNotes_ReturnAllMarkedNotes()
    {
        // Arrange
        var tracker = new DirtyTracker();
        var noteIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        foreach (var id in noteIds)
            tracker.MarkAsDirty(id);

        // Act
        var dirtyNotes = tracker.GetDirtyNotes().ToList();

        // Assert
        Assert.AreEqual(3, dirtyNotes.Count);
        foreach (var id in noteIds)
            Assert.IsTrue(dirtyNotes.Contains(id));
    }

    [TestMethod]
    public void HasPendingChanges_ReturnsTrueWhenDirty()
    {
        // Arrange
        var tracker = new DirtyTracker();

        // Act & Assert
        Assert.IsFalse(tracker.HasPendingChanges);

        tracker.MarkAsDirty(Guid.NewGuid());
        Assert.IsTrue(tracker.HasPendingChanges);
    }

    [TestMethod]
    public void ClearAll_RemovesAllDirtyNotes()
    {
        // Arrange
        var tracker = new DirtyTracker();
        tracker.MarkAsDirty(Guid.NewGuid());
        tracker.MarkAsDirty(Guid.NewGuid());

        // Act
        tracker.ClearAll();

        // Assert
        Assert.IsFalse(tracker.HasPendingChanges);
        Assert.AreEqual(0, tracker.GetDirtyNotes().Count());
    }

    [TestMethod]
    public void IsDirty_ReturnsFalseForUnmarkedNote()
    {
        // Arrange
        var tracker = new DirtyTracker();
        var noteId = Guid.NewGuid();

        // Act & Assert
        Assert.IsFalse(tracker.IsDirty(noteId));
    }

    [TestMethod]
    public void MarkAsDirty_IsThreadSafe()
    {
        // Arrange
        var tracker = new DirtyTracker();
        var noteIds = new List<Guid>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var id = Guid.NewGuid();
            noteIds.Add(id);
            tasks.Add(Task.Run(() => tracker.MarkAsDirty(id)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var dirtyNotes = tracker.GetDirtyNotes().ToList();
        Assert.AreEqual(100, dirtyNotes.Count);
    }
}
