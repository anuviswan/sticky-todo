using System.Windows;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// The bounds a sticky note window should actually be shown at, together with whether those
/// bounds had to be corrected because the saved ones fell outside every connected monitor.
/// Callers use <see cref="WasCorrected"/> to avoid persisting a correction back over the
/// position the user originally chose - see <c>StickyNoteWindowService</c>.
/// </summary>
public readonly record struct NoteWindowPlacement(Rect Bounds, bool WasCorrected);

/// <summary>
/// Pure geometry for placing a sticky note window on a currently-connected monitor, kept free of
/// any WPF Window or screen-enumeration dependency so it can be unit tested directly.
/// A note's position is persisted from the machine and display layout it was last closed on, so a
/// note last placed on a second monitor is invisible when the app next runs without that monitor -
/// it opens, but entirely off-screen, which reads to the user as "the note won't open".
/// </summary>
public static class NoteWindowPlacementCalculator
{
    /// <summary>
    /// Width (in DIPs) of the note that must remain on a monitor for it to count as reachable.
    /// A note narrower than this only needs to be fully visible.
    /// </summary>
    public const double MinimumVisibleWidth = 120;

    /// <summary>
    /// Height (in DIPs) of the note that must remain on a monitor for it to count as reachable.
    /// Sized so at least the draggable title bar is grabbable.
    /// </summary>
    public const double MinimumVisibleHeight = 40;

    /// <summary>
    /// Returns bounds guaranteed to be reachable on one of <paramref name="workAreas"/>.
    /// <paramref name="saved"/> is returned untouched when it already overlaps some work area by at
    /// least <see cref="MinimumVisibleWidth"/> x <see cref="MinimumVisibleHeight"/> (or by its own
    /// size, when it is smaller than that), and when no work areas are supplied at all. Otherwise the
    /// note is shrunk to fit and moved fully inside the nearest work area, preserving its size where
    /// possible, and the result is flagged with <see cref="NoteWindowPlacement.WasCorrected"/>.
    /// </summary>
    /// <param name="saved">The bounds persisted for the note, in device-independent units.</param>
    /// <param name="workAreas">Work areas of the connected monitors, in device-independent units.</param>
    public static NoteWindowPlacement EnsureVisible(Rect saved, IReadOnlyList<Rect> workAreas)
    {
        ArgumentNullException.ThrowIfNull(workAreas);

        if (workAreas.Count == 0)
            return new NoteWindowPlacement(saved, WasCorrected: false);

        if (!IsFinite(saved))
            return new NoteWindowPlacement(CenterIn(workAreas[0], FallbackSize(saved, workAreas[0])), WasCorrected: true);

        if (IsReachable(saved, workAreas))
            return new NoteWindowPlacement(saved, WasCorrected: false);

        var target = FindNearestWorkArea(saved, workAreas);
        return new NoteWindowPlacement(ClampInto(saved, target), WasCorrected: true);
    }

    /// <summary>
    /// True when enough of <paramref name="bounds"/> overlaps a single work area for the user to
    /// see and drag the note.
    /// </summary>
    private static bool IsReachable(Rect bounds, IReadOnlyList<Rect> workAreas)
    {
        var requiredWidth = Math.Min(MinimumVisibleWidth, bounds.Width);
        var requiredHeight = Math.Min(MinimumVisibleHeight, bounds.Height);

        foreach (var workArea in workAreas)
        {
            var overlap = Rect.Intersect(bounds, workArea);
            if (overlap.IsEmpty)
                continue;

            if (overlap.Width >= requiredWidth && overlap.Height >= requiredHeight)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Picks the work area to move the note onto: the one it overlaps most, or - when it overlaps
    /// none - the one whose centre is closest, so the note surfaces on the monitor nearest to where
    /// the user last left it rather than always on the primary display.
    /// </summary>
    private static Rect FindNearestWorkArea(Rect bounds, IReadOnlyList<Rect> workAreas)
    {
        var best = workAreas[0];
        var bestOverlap = 0d;
        var bestDistance = double.MaxValue;

        foreach (var workArea in workAreas)
        {
            var overlap = Rect.Intersect(bounds, workArea);
            var overlapArea = overlap.IsEmpty ? 0 : overlap.Width * overlap.Height;

            if (overlapArea > bestOverlap)
            {
                best = workArea;
                bestOverlap = overlapArea;
                continue;
            }

            if (bestOverlap > 0 || overlapArea > 0)
                continue;

            var distance = DistanceBetweenCenters(bounds, workArea);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = workArea;
            }
        }

        return best;
    }

    private static double DistanceBetweenCenters(Rect first, Rect second)
    {
        var deltaX = (first.Left + first.Width / 2) - (second.Left + second.Width / 2);
        var deltaY = (first.Top + first.Height / 2) - (second.Top + second.Height / 2);
        return deltaX * deltaX + deltaY * deltaY;
    }

    /// <summary>
    /// Shrinks <paramref name="bounds"/> to fit <paramref name="workArea"/> if needed, then moves it
    /// so it sits entirely inside that work area.
    /// </summary>
    private static Rect ClampInto(Rect bounds, Rect workArea)
    {
        var width = Math.Min(bounds.Width, workArea.Width);
        var height = Math.Min(bounds.Height, workArea.Height);

        var left = Math.Clamp(bounds.Left, workArea.Left, workArea.Right - width);
        var top = Math.Clamp(bounds.Top, workArea.Top, workArea.Bottom - height);

        return new Rect(left, top, width, height);
    }

    private static Rect CenterIn(Rect workArea, Size size)
    {
        var left = workArea.Left + (workArea.Width - size.Width) / 2;
        var top = workArea.Top + (workArea.Height - size.Height) / 2;
        return new Rect(left, top, size.Width, size.Height);
    }

    /// <summary>
    /// Size to fall back to when the persisted bounds are unusable (NaN/Infinity), preserving
    /// whichever dimension is still a sane number.
    /// </summary>
    private static Size FallbackSize(Rect bounds, Rect workArea)
    {
        var width = IsUsableLength(bounds.Width) ? Math.Min(bounds.Width, workArea.Width) : Math.Min(320, workArea.Width);
        var height = IsUsableLength(bounds.Height) ? Math.Min(bounds.Height, workArea.Height) : Math.Min(400, workArea.Height);
        return new Size(width, height);
    }

    private static bool IsFinite(Rect bounds) =>
        double.IsFinite(bounds.Left) && double.IsFinite(bounds.Top) &&
        IsUsableLength(bounds.Width) && IsUsableLength(bounds.Height);

    private static bool IsUsableLength(double length) => double.IsFinite(length) && length > 0;
}
