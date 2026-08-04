namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Abstraction over opening a URL in the user's default browser. Keeps ViewModels view-agnostic
/// and enables testing.
/// </summary>
public interface IUrlLauncherService
{
    /// <summary>
    /// Opens the given URL in the user's default browser.
    /// </summary>
    void OpenUrl(string url);
}
