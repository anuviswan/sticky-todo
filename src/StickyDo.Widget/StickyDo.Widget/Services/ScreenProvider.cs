using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Utilities;
using Application = System.Windows.Application;

namespace StickyDo.Widget.Services;

/// <summary>
/// Enumerates the connected monitors' work areas via <see cref="Screen.AllScreens"/>, converting
/// them from physical pixels to device-independent units the same way
/// <see cref="Behaviors.ResizeWindowBehavior"/> does, so the values can be compared directly
/// against <see cref="Window.Left"/>/<see cref="Window.Top"/>.
/// </summary>
public class ScreenProvider : IScreenProvider
{
    public IReadOnlyList<Rect> GetWorkAreasInDips()
    {
        try
        {
            var transform = GetDeviceToDipTransform();

            return Screen.AllScreens
                .Select(screen => ToDips(screen.WorkingArea, transform))
                .ToList();
        }
        catch (Exception ex)
        {
            // Screen enumeration can fail during a display change; callers treat an empty list as
            // "unknown layout" and leave window bounds untouched rather than guessing.
            LoggerHelper.LogException(ex, nameof(GetWorkAreasInDips));
            return [];
        }
    }

    private static Rect ToDips(System.Drawing.Rectangle workArea, Matrix transform)
    {
        var topLeft = transform.Transform(new Point(workArea.Left, workArea.Top));
        var bottomRight = transform.Transform(new Point(workArea.Right, workArea.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    /// <summary>
    /// Resolves the physical-pixel to device-independent-unit transform from any live window,
    /// falling back to the identity matrix (i.e. 100% scaling) when no window has a presentation
    /// source yet.
    /// </summary>
    private static Matrix GetDeviceToDipTransform()
    {
        var window = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => new WindowInteropHelper(w).Handle != IntPtr.Zero);
        if (window is null)
            return Matrix.Identity;

        var source = PresentationSource.FromVisual(window);
        return source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
    }
}
