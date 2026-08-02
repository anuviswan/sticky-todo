namespace StickyDo.Domain.Models;

/// <summary>
/// Metadata describing a notes backup archive, stored as a JSON file at the root of the zip.
/// </summary>
public class BackupManifest
{
    /// <summary>Version of the app that produced this backup (e.g. "1.0.0").</summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the backup was created.</summary>
    public DateTime ExportedAtUtc { get; set; }

    /// <summary>Number of note files included in the backup.</summary>
    public int NoteCount { get; set; }
}
