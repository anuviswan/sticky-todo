using System.Windows;
using StickyDo.Widget.Interfaces;

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

    public void RequestShow()
    {
        if (_mainWindow == null)
            return;

        if (_mainWindow.WindowState == System.Windows.WindowState.Minimized)
            _mainWindow.WindowState = System.Windows.WindowState.Normal;

        if (!_mainWindow.IsVisible)
            _mainWindow.Show();

        _mainWindow.Activate();
    }
}
