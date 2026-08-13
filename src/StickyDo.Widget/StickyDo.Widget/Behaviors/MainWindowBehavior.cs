using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Behavior to handle MainWindow-specific operations in an MVVM-friendly way.
/// </summary>
public static class MainWindowBehavior
{
    /// <summary>
    /// Attaches the behavior to the main window.
    /// </summary>
    public static void AttachToWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        // Wire up title bar drag and double-click-to-maximize/restore
        var titleBar = window.FindName("TitleBarBorder") as Border;
        if (titleBar != null)
        {
            titleBar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    window.WindowState = window.WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                    return;
                }

                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    try
                    {
                        window.DragMove();
                    }
                    catch
                    {
                        // DragMove can throw if called at wrong time
                    }
                }
            };
        }

        // Wire up window loaded event to load notes
        window.Loaded += (s, e) =>
        {
            if (window.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.LoadNotesCommand.Execute(null);
            }
        };

        // WindowStyle="None" opts the window out of DWM's normal maximize handling, so without
        // this hook WindowState.Maximized would grow the window to the full monitor bounds and
        // cover the taskbar. Intercepting WM_GETMINMAXINFO constrains it to the monitor's work
        // area instead, matching standard Windows maximize behavior on any monitor.
        window.SourceInitialized += (s, e) =>
        {
            if (PresentationSource.FromVisual(window) is HwndSource hwndSource)
            {
                hwndSource.AddHook(WindowProc);
            }
        };

        // Border.CornerRadius only rounds the border's own background/outline, not its
        // children, so the root content is clipped explicitly to match the rounded window.
        // The corner radius collapses to 0 while maximized (see MainWindow.xaml), so the clip
        // must follow suit or it would keep rounding corners that should now be square.
        var rootBorder = window.FindName("RootBorder") as Border;
        if (rootBorder != null)
        {
            void UpdateClip()
            {
                var radius = window.WindowState == WindowState.Maximized ? 0 : 4;
                rootBorder.Clip = new RectangleGeometry(
                    new Rect(0, 0, rootBorder.ActualWidth, rootBorder.ActualHeight), radius, radius);
            }

            rootBorder.SizeChanged += (s, e) => UpdateClip();
            window.StateChanged += (s, e) => UpdateClip();
            UpdateClip();
        }
    }

    private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_GETMINMAXINFO = 0x0024;

        if (msg == WM_GETMINMAXINFO)
        {
            ApplyWorkAreaToMinMaxInfo(hwnd, lParam);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static void ApplyWorkAreaToMinMaxInfo(IntPtr hwnd, IntPtr lParam)
    {
        const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero)
            return;

        var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
            return;

        var workArea = monitorInfo.rcWork;
        var monitorArea = monitorInfo.rcMonitor;

        var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
        minMaxInfo.ptMaxPosition.X = Math.Abs(workArea.Left - monitorArea.Left);
        minMaxInfo.ptMaxPosition.Y = Math.Abs(workArea.Top - monitorArea.Top);
        minMaxInfo.ptMaxSize.X = Math.Abs(workArea.Right - workArea.Left);
        minMaxInfo.ptMaxSize.Y = Math.Abs(workArea.Bottom - workArea.Top);
        Marshal.StructureToPtr(minMaxInfo, lParam, true);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }
}
