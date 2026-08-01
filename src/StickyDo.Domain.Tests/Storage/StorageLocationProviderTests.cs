using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Storage;

namespace StickyDo.Domain.Tests.Storage;

[TestClass]
public class StorageLocationProviderTests
{
    [TestMethod]
    public void RootDirectory_ForReleaseBuild_HasNoDebugSuffix()
    {
        var provider = new StorageLocationProvider(isDebugBuild: false);

        Assert.IsTrue(provider.RootDirectory.EndsWith(@"DefineStack\StickyDO"));
        Assert.IsFalse(provider.RootDirectory.EndsWith("StickyDO.Debug"));
    }

    [TestMethod]
    public void RootDirectory_ForDebugBuild_HasDebugSuffix()
    {
        var provider = new StorageLocationProvider(isDebugBuild: true);

        Assert.IsTrue(provider.RootDirectory.EndsWith(@"DefineStack\StickyDO.Debug"));
    }

    [TestMethod]
    public void RootDirectory_DebugAndRelease_ResolveToDifferentLocations()
    {
        var releaseProvider = new StorageLocationProvider(isDebugBuild: false);
        var debugProvider = new StorageLocationProvider(isDebugBuild: true);

        Assert.AreNotEqual(releaseProvider.RootDirectory, debugProvider.RootDirectory);
    }

    [TestMethod]
    public void RootDirectory_IsUnderLocalAppData()
    {
        var expectedRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var provider = new StorageLocationProvider(isDebugBuild: false);

        Assert.IsTrue(provider.RootDirectory.StartsWith(expectedRoot));
    }

    [TestMethod]
    public void SubDirectories_NestUnderRootDirectory()
    {
        var provider = new StorageLocationProvider(isDebugBuild: false);

        Assert.AreEqual(Path.Combine(provider.RootDirectory, "Data"), provider.DataDirectory);
        Assert.AreEqual(Path.Combine(provider.RootDirectory, "Settings"), provider.SettingsDirectory);
        Assert.AreEqual(Path.Combine(provider.RootDirectory, "Logs"), provider.LogsDirectory);
        Assert.AreEqual(Path.Combine(provider.RootDirectory, "Backups"), provider.BackupsDirectory);
    }

    [TestMethod]
    public void DefaultConstructor_ResolvesWithoutThrowing()
    {
        var provider = new StorageLocationProvider();

        Assert.IsFalse(string.IsNullOrWhiteSpace(provider.RootDirectory));
    }
}
