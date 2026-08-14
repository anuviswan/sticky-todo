using System.Windows;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Pure geometry calculation for edge/corner window resizing, kept free of any WPF Window or
/// input-event dependency so it can be unit tested directly. <see cref="ResizeWindowBehavior"/>
/// supplies the live window bounds, drag delta, size constraints, and work-area bounds; this
/// class only computes the resulting rectangle.
/// </summary>
public static class WindowResizeCalculator
{
    /// <summary>
    /// Computes the new window bounds for a drag of <paramref name="deltaX"/>/<paramref name="deltaY"/>
    /// (in DIPs, measured from the drag's starting point) applied to <paramref name="edge"/>, given the
    /// bounds the window had when the drag started. Only the dimension(s) implied by
    /// <paramref name="edge"/> change: a single edge changes one dimension, a corner changes both.
    /// The result is clamped so the window never shrinks below <paramref name="minWidth"/>/
    /// <paramref name="minHeight"/>, never grows past <paramref name="maxWidth"/>/<paramref name="maxHeight"/>,
    /// and never extends outside <paramref name="workArea"/>.
    /// </summary>
    public static Rect Calculate(
        Rect startBounds,
        ResizeEdge edge,
        double deltaX,
        double deltaY,
        double minWidth,
        double minHeight,
        double maxWidth,
        double maxHeight,
        Rect workArea)
    {
        var left = startBounds.Left;
        var top = startBounds.Top;
        var width = startBounds.Width;
        var height = startBounds.Height;

        var affectsLeft = edge is ResizeEdge.Left or ResizeEdge.TopLeft or ResizeEdge.BottomLeft;
        var affectsRight = edge is ResizeEdge.Right or ResizeEdge.TopRight or ResizeEdge.BottomRight;
        var affectsTop = edge is ResizeEdge.Top or ResizeEdge.TopLeft or ResizeEdge.TopRight;
        var affectsBottom = edge is ResizeEdge.Bottom or ResizeEdge.BottomLeft or ResizeEdge.BottomRight;

        if (affectsRight)
        {
            width = Clamp(startBounds.Width + deltaX, minWidth, maxWidth);
            width = Math.Min(width, workArea.Right - startBounds.Left);
        }

        if (affectsBottom)
        {
            height = Clamp(startBounds.Height + deltaY, minHeight, maxHeight);
            height = Math.Min(height, workArea.Bottom - startBounds.Top);
        }

        if (affectsLeft)
        {
            // The right edge stays fixed; only the left edge moves, so derive it from the
            // clamped width rather than clamping the raw dragged position directly.
            var rightEdge = startBounds.Left + startBounds.Width;
            var clampedWidth = Clamp(startBounds.Width - deltaX, minWidth, maxWidth);
            left = Math.Max(rightEdge - clampedWidth, workArea.Left);
            width = rightEdge - left;
        }

        if (affectsTop)
        {
            // The bottom edge stays fixed; only the top edge moves, so derive it from the
            // clamped height rather than clamping the raw dragged position directly.
            var bottomEdge = startBounds.Top + startBounds.Height;
            var clampedHeight = Clamp(startBounds.Height - deltaY, minHeight, maxHeight);
            top = Math.Max(bottomEdge - clampedHeight, workArea.Top);
            height = bottomEdge - top;
        }

        return new Rect(left, top, width, height);
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(value, max));
}
