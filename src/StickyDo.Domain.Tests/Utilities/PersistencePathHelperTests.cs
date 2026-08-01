using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Storage;
using StickyDo.Domain.Utilities;

namespace StickyDo.Domain.Tests.Utilities;

[TestClass]
public class PersistencePathHelperTests
{
    private string _testRootDirectory = null!;
    private PersistencePathHelper _pathHelper = null!;

    [TestInitialize]
    public void Setup()
    {
        _testRootDirectory = Path.Combine(Path.GetTempPath(), "StickyDo_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRootDirectory);
        _pathHelper = new PersistencePathHelper(new FakeStorageLocationProvider(_testRootDirectory));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testRootDirectory))
            Directory.Delete(_testRootDirectory, recursive: true);
    }

    [TestMethod]
    public void GetDataDirectoryPath_ReturnsProviderDataDirectory()
    {
        // Act
        var path = _pathHelper.GetDataDirectoryPath();

        // Assert
        Assert.AreEqual(Path.Combine(_testRootDirectory, "Data"), path);
    }

    [TestMethod]
    public void GetNoteFilePath_ReturnsCorrectFormat()
    {
        // Arrange
        var noteId = Guid.NewGuid();

        // Act
        var path = _pathHelper.GetNoteFilePath(noteId);

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
        var path = _pathHelper.GetNoteTemporaryFilePath(noteId);

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
        var path = _pathHelper.GetNoteCorruptFilePath(noteId);

        // Assert
        Assert.IsTrue(path.EndsWith(".json.corrupt"));
        Assert.IsTrue(path.Contains(noteId.ToString("N")));
    }

    [TestMethod]
    public void ExtractNoteIdFromFilePath_ParsesValidPath()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var filePath = _pathHelper.GetNoteFilePath(noteId);

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
        var invalidPath = "C:\\StickyDo\\notavalid.json";

        // Act
        var extractedId = PersistencePathHelper.ExtractNoteIdFromFilePath(invalidPath);

        // Assert
        Assert.IsNull(extractedId);
    }

    [TestMethod]
    public void EnsureDataDirectoryExists_CreatesDirectory()
    {
        // Act
        _pathHelper.EnsureDataDirectoryExists();

        // Assert
        var path = _pathHelper.GetDataDirectoryPath();
        Assert.IsTrue(Directory.Exists(path));
    }

    [TestMethod]
    public void GetAllNoteFiles_ReturnsValidJsonFiles()
    {
        // Arrange
        _pathHelper.EnsureDataDirectoryExists();

        var testNoteId = Guid.NewGuid();
        var testFilePath = _pathHelper.GetNoteFilePath(testNoteId);
        File.WriteAllText(testFilePath, "{}");

        // Act
        var files = _pathHelper.GetAllNoteFiles().ToList();

        // Assert
        Assert.AreEqual(1, files.Count);
        Assert.IsTrue(files[0].EndsWith(".json"));
    }

    private sealed class FakeStorageLocationProvider : IStorageLocationProvider
    {
        public FakeStorageLocationProvider(string root)
        {
            RootDirectory = root;
            DataDirectory = Path.Combine(root, "Data");
            SettingsDirectory = Path.Combine(root, "Settings");
            LogsDirectory = Path.Combine(root, "Logs");
            BackupsDirectory = Path.Combine(root, "Backups");
        }

        public string RootDirectory { get; }
        public string DataDirectory { get; }
        public string SettingsDirectory { get; }
        public string LogsDirectory { get; }
        public string BackupsDirectory { get; }
    }
}
