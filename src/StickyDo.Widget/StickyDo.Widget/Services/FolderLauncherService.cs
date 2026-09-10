using System.Diagnostics;
using System.IO;
using StickyDo.Widget.Interfaces;

namespace StickyDo.Widget.Services;

/// <summary>
/// WPF implementation of <see cref="IFolderLauncherService"/> using the OS shell to open a folder
/// in File Explorer.
/// </summary>
public class FolderLauncherService : IFolderLauncherService
{
    public void OpenFolder(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
