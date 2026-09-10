namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Abstraction over revealing a folder in the Windows shell. Kept separate from
/// <see cref="IUrlLauncherService"/> so neither abstraction takes on a second responsibility, and
/// so ViewModels stay view-agnostic and testable.
/// </summary>
public interface IFolderLauncherService
{
    /// <summary>
    /// Opens the given folder in File Explorer, creating it first if it does not exist yet
    /// (the notes folder is only created lazily on first save).
    /// </summary>
    void OpenFolder(string path);
}
