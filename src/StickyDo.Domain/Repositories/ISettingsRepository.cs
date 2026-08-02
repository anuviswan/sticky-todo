using StickyDo.Domain.Models;

namespace StickyDo.Domain.Repositories;

/// <summary>
/// Loads and saves the user's persisted <see cref="AppSettings"/>.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Loads settings from disk. Returns default settings if no settings file exists yet,
    /// or if the persisted file is missing, corrupt, or contains invalid values.
    /// </summary>
    Task<AppSettings> LoadAsync();

    /// <summary>
    /// Persists the given settings to disk, overwriting any previous value.
    /// </summary>
    Task SaveAsync(AppSettings settings);
}
