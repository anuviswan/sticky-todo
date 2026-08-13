using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Screen = System.Windows.Forms.Screen;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Attached behavior that resizes a window by dragging a specific UIElement, growing/shrinking
/// from the edge or corner given by <see cref="EdgeProperty"/>. Attach one instance per resize
/// handle (e.g. thin Border/Rectangle elements laid over the window's edges and corners).
/// Mirrors the shape of <see cref="DragWindowBehavior"/>: a bindable enable flag toggles the
/// mouse hooks, so XAML can disable resizing the same way it disables dragging (e.g. while a
/// note is pinned).
/// </summary>
public static class ResizeWindowBehavior
{
    private sealed class DragState
    {
        public bool IsResizing;
        public Point StartMouseScreen;
        public Rect StartBounds;
        public Rect WorkArea;
        public DpiScale Dpi;
    }

    private static readonly ConditionalWeakTable<UIElement, DragState> States = new();

    public static ResizeEdge GetEdge(DependencyObject obj) => (ResizeEdge)obj.GetValue(EdgeProperty);
    public static void SetEdge(DependencyObject obj, ResizeEdge value) => obj.SetValue(EdgeProperty, value);

    public static readonly DependencyProperty EdgeProperty =
        DependencyProperty.RegisterAttached(
            "Edge",
            typeof(ResizeEdge),
            typeof(ResizeWindowBehavior),
            new PropertyMetadata(ResizeEdge.BottomRight));

    public static bool GetCanResize(DependencyObject obj) => (bool)obj.GetValue(CanResizeProperty);
    public static void SetCanResize(DependencyObject obj, bool value) => obj.SetValue(CanResizeProperty, value);

    public static readonly DependencyProperty CanResizeProperty =
        DependencyProperty.RegisterAttached(
            "CanResize",
            typeof(bool),
            typeof(ResizeWindowBehavior),
            new PropertyMetadata(false, OnCanResizeChanged));

    private static void OnCanResizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;

        element.MouseLeftButtonDown -= Element_MouseLeftButtonDown;
        element.MouseMove -= Element_MouseMove;
        element.MouseLeftButtonUp -= Element_MouseLeftButtonUp;

        if ((bool)e.NewValue)
        {
            element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            element.MouseMove += Element_MouseMove;
            element.MouseLeftButtonUp += Element_MouseLeftButtonUp;
        }
    }

    private static void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not UIElement element)
            return;

        var window = Window.GetWindow(element);
        if (window is null)
            return;

        var state = States.GetOrCreateValue(element);
        state.IsResizing = true;
        state.StartMouseScreen = element.PointToScreen(e.GetPosition(element));
        state.StartBounds = new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight);
        state.WorkArea = GetWorkAreaInDips(window);
        state.Dpi = VisualTreeHelper.GetDpi(element);

        element.CaptureMouse();
        e.Handled = true;
    }

    private static void Element_MouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not UIElement element)
            return;

        if (!States.TryGetValue(element, out var state) || !state.IsResizing)
            return;

        var window = Window.GetWindow(element);
        if (window is null)
            return;

        // PointToScreen returns physical device pixels, not the DIPs that Window.Left/Top/Width/
        // Height use, so the raw pixel delta must be divided back down by the visual's DPI scale
        // before it's applied to the window bounds - otherwise resizing runs faster than the
        // cursor on any monitor scaled above 100%.
        var currentScreen = element.PointToScreen(e.GetPosition(element));
        var deltaX = (currentScreen.X - state.StartMouseScreen.X) / state.Dpi.DpiScaleX;
        var deltaY = (currentScreen.Y - state.StartMouseScreen.Y) / state.Dpi.DpiScaleY;

        var newBounds = WindowResizeCalculator.Calculate(
            state.StartBounds,
            GetEdge(element),
            deltaX,
            deltaY,
            window.MinWidth,
            window.MinHeight,
            window.MaxWidth,
            window.MaxHeight,
            state.WorkArea);

        window.Left = newBounds.Left;
        window.Top = newBounds.Top;
        window.Width = newBounds.Width;
        window.Height = newBounds.Height;
    }

    private static void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not UIElement element)
            return;

        if (States.TryGetValue(element, out var state))
            state.IsResizing = false;

        element.ReleaseMouseCapture();
    }

    /// <summary>
    /// Resolves the working area (excludes the taskbar) of the monitor the window currently sits
    /// on, converted from physical pixels to the window's device-independent units. The app is
    /// Per-Monitor-V2 DPI aware (the SDK's default WPF manifest), so this stays accurate when a
    /// note is dragged between monitors with different scale factors.
    /// </summary>
    private static Rect GetWorkAreaInDips(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var workArea = Screen.FromHandle(hwnd).WorkingArea;

        var source = PresentationSource.FromVisual(window);
        if (source?.CompositionTarget is null)
            return new Rect(workArea.Left, workArea.Top, workArea.Width, workArea.Height);

        var transform = source.CompositionTarget.TransformFromDevice;
        var topLeft = transform.Transform(new Point(workArea.Left, workArea.Top));
        var bottomRight = transform.Transform(new Point(workArea.Right, workArea.Bottom));
        return new Rect(topLeft, bottomRight);
    }
}
