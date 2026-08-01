namespace StickyDo.Domain.Storage;

/// <summary>
/// Resolves the application's storage root based on build configuration.
/// This is the only component containing build-specific storage logic;
/// Release builds resolve to %LocalAppData%\DefineStack\StickyDO and
/// Debug builds resolve to %LocalAppData%\DefineStack\StickyDO.Debug,
/// keeping developer data fully isolated from production data.
/// </summary>
public sealed class StorageLocationProvider : IStorageLocationProvider
{
    private const string CompanyFolderName = "DefineStack";
    private const string ProductFolderName = "StickyDO";
    private const string DebugSuffix = ".Debug";

    public string RootDirectory { get; }
    public string DataDirectory { get; }
    public string SettingsDirectory { get; }
    public string LogsDirectory { get; }
    public string BackupsDirectory { get; }

    public StorageLocationProvider() : this(IsDebugBuild)
    {
    }

    /// <summary>
    /// Internal constructor allowing tests to exercise both the Debug and Release
    /// resolution branches deterministically, independent of the test assembly's
    /// own build configuration.
    /// </summary>
    internal StorageLocationProvider(bool isDebugBuild)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var productFolderName = isDebugBuild ? ProductFolderName + DebugSuffix : ProductFolderName;

        RootDirectory = Path.Combine(localAppData, CompanyFolderName, productFolderName);
        DataDirectory = Path.Combine(RootDirectory, "Data");
        SettingsDirectory = Path.Combine(RootDirectory, "Settings");
        LogsDirectory = Path.Combine(RootDirectory, "Logs");
        BackupsDirectory = Path.Combine(RootDirectory, "Backups");
    }

    private static bool IsDebugBuild
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
