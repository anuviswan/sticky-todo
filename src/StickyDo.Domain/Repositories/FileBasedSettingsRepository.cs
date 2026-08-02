using System.Text.Json;
using StickyDo.Domain.Constants;
using StickyDo.Domain.Models;
using StickyDo.Domain.Serialization;
using StickyDo.Domain.Storage;

namespace StickyDo.Domain.Repositories;

/// <summary>
/// File-based implementation of <see cref="ISettingsRepository"/>. Persists a single
/// <see cref="AppSettings"/> JSON file under the directory resolved by the injected
/// <see cref="IStorageLocationProvider"/>, using the same atomic-write and corrupt-file
/// handling conventions as <see cref="FileBasedRepository"/>.
/// </summary>
public class FileBasedSettingsRepository : ISettingsRepository
{
    private const string SettingsFileName = "settings.json";

    private readonly IStorageLocationProvider _storageLocationProvider;

    public FileBasedSettingsRepository(IStorageLocationProvider storageLocationProvider)
    {
        _storageLocationProvider = storageLocationProvider ?? throw new ArgumentNullException(nameof(storageLocationProvider));
    }

    public async Task<AppSettings> LoadAsync()
    {
        var filePath = GetSettingsFilePath();

        if (!File.Exists(filePath))
            return new AppSettings();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonSerializationOptions.Default) ?? new AppSettings();

            if (!ColorPalette.Colors.Contains(settings.DefaultNoteColor))
                settings.DefaultNoteColor = ColorPalette.GetDefaultColor();

            return settings;
        }
        catch (JsonException)
        {
            HandleCorruptedFile(filePath);
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        EnsureSettingsDirectoryExists();

        var json = JsonSerializer.Serialize(settings, JsonSerializationOptions.Default);
        await AtomicFileWriter.WriteAtomicAsync(GetSettingsFilePath(), json);
    }

    private string GetSettingsFilePath() =>
        Path.Combine(_storageLocationProvider.SettingsDirectory, SettingsFileName);

    private void EnsureSettingsDirectoryExists()
    {
        var settingsDir = _storageLocationProvider.SettingsDirectory;
        if (!Directory.Exists(settingsDir))
            Directory.CreateDirectory(settingsDir);
    }

    /// <summary>
    /// Handles a corrupted settings file by renaming it for manual recovery, mirroring
    /// <see cref="FileBasedRepository"/>'s handling of corrupted note files.
    /// </summary>
    private static void HandleCorruptedFile(string filePath)
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
}
