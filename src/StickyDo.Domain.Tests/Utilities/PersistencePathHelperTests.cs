using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Utilities;

namespace StickyDo.Domain.Tests.Utilities;

[TestClass]
public class PersistencePathHelperTests
{
    [TestMethod]
    public void GetDataDirectoryPath_ReturnsValidPath()
    {
        // Act
        var path = PersistencePathHelper.GetDataDirectoryPath();

        // Assert
        Assert.IsNotNull(path);
        Assert.IsTrue(path.Contains("StickyTODO"));
        Assert.IsTrue(path.Contains("LocalAppData") || path.Contains("AppData"));
    }

    [TestMethod]
    public void GetNoteFilePath_ReturnsCorrectFormat()
    {
        // Arrange
        var noteId = Guid.NewGuid();

        // Act
        var path = PersistencePathHelper.GetNoteFilePath(noteId);

        // Assert
        Assert.IsTrue(path.EndsWith(".json"));
        Assert.IsTrue(path.Contains(noteId.ToString("N")));
    }

    [TestMethod]
    public void GetNoteTemporaryFilePath_ReturnsCorrectFormat()
    {
        // Arrange
        var noteId = Guid.NewGuid();

        // Act
        var path = PersistencePathHelper.GetNoteTemporaryFilePath(noteId);

        // Assert
        Assert.IsTrue(path.EndsWith(".json.tmp"));
        Assert.IsTrue(path.Contains(noteId.ToString("N")));
    }

    [TestMethod]
    public void GetNoteCorruptFilePath_ReturnsCorrectFormat()
    {
        // Arrange
        var noteId = Guid.NewGuid();

        // Act
        var path = PersistencePathHelper.GetNoteCorruptFilePath(noteId);

        // Assert
        Assert.IsTrue(path.EndsWith(".json.corrupt"));
        Assert.IsTrue(path.Contains(noteId.ToString("N")));
    }

    [TestMethod]
    public void ExtractNoteIdFromFilePath_ParsesValidPath()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var filePath = PersistencePathHelper.GetNoteFilePath(noteId);

        // Act
        var extractedId = PersistencePathHelper.ExtractNoteIdFromFilePath(filePath);

        // Assert
        Assert.IsNotNull(extractedId);
        Assert.AreEqual(noteId, extractedId.Value);
    }

    [TestMethod]
    public void ExtractNoteIdFromFilePath_ReturnsNullForInvalidPath()
    {
        // Arrange
        var invalidPath = "C:\\StickyTODO\\notavalid.json";

        // Act
        var extractedId = PersistencePathHelper.ExtractNoteIdFromFilePath(invalidPath);

        // Assert
        Assert.IsNull(extractedId);
    }

    [TestMethod]
    public void EnsureDataDirectoryExists_CreatesDirectory()
    {
        // Act
        PersistencePathHelper.EnsureDataDirectoryExists();

        // Assert
        var path = PersistencePathHelper.GetDataDirectoryPath();
        Assert.IsTrue(Directory.Exists(path));
    }

    [TestMethod]
    public void GetAllNoteFiles_ReturnsValidJsonFiles()
    {
        // Arrange
        var dataDir = PersistencePathHelper.GetDataDirectoryPath();
        if (Directory.Exists(dataDir))
            Directory.Delete(dataDir, recursive: true);

        PersistencePathHelper.EnsureDataDirectoryExists();

        var testNoteId = Guid.NewGuid();
        var testFilePath = PersistencePathHelper.GetNoteFilePath(testNoteId);
        File.WriteAllText(testFilePath, "{}");

        // Act
        var files = PersistencePathHelper.GetAllNoteFiles().ToList();

        // Assert
        Assert.AreEqual(1, files.Count);
        Assert.IsTrue(files[0].EndsWith(".json"));

        // Cleanup
        File.Delete(testFilePath);
    }
}
