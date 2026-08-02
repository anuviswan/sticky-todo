using StickyDo.Domain.Constants;

namespace StickyDo.Domain.Models;

/// <summary>
/// User-configurable application settings, persisted as a single JSON file.
/// </summary>
public class AppSettings
{
    public bool LaunchAtStartup { get; set; }

    public uint DefaultNoteColor { get; set; } = ColorPalette.GetDefaultColor();
}
