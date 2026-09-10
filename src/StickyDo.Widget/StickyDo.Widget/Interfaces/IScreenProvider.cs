using System.Windows;

namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Abstraction over the connected monitors' work areas. Keeps screen enumeration out of the
/// services that place windows so their placement logic stays testable.
/// </summary>
public interface IScreenProvider
{
    /// <summary>
    /// Gets the work area (the monitor bounds excluding the taskbar) of every connected monitor,
    /// expressed in the device-independent units used by <see cref="Window.Left"/> and friends.
    /// Returns an empty list if the monitors cannot be enumerated.
    /// </summary>
    IReadOnlyList<Rect> GetWorkAreasInDips();
}
