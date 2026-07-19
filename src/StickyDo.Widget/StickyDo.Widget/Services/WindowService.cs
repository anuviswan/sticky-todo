using System.Windows;

namespace StickyDo.Widget.Services;

/// <summary>
/// WPF implementation of IWindowService.
/// Delegates window operations to the main window.
/// </summary>
public class WindowService : IWindowService
{
    private Window? _mainWindow;

    public void SetMainWindow(Window window)
    {
        _mainWindow = window ?? throw new ArgumentNullException(nameof(window));
    }

    public void RequestMinimize()
    {
        if (_mainWindow != null)
            _mainWindow.WindowState = System.Windows.WindowState.Minimized;
    }

    public void RequestClose()
    {
        _mainWindow?.Close();
    }
}
