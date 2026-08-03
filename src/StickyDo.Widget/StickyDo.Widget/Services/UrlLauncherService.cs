using System.Diagnostics;
using StickyDo.Widget.Interfaces;

namespace StickyDo.Widget.Services;

/// <summary>
/// WPF implementation of <see cref="IUrlLauncherService"/> using the OS shell to open URLs in the
/// user's default browser.
/// </summary>
public class UrlLauncherService : IUrlLauncherService
{
    public void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
