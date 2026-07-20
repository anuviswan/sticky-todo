using System.Windows;
using System.Windows.Input;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Attached behavior that allows dragging a window from a specific UIElement.
/// Attach to title bar or drag handle.
/// </summary>
public static class DragWindowBehavior
{
    public static bool GetCanDrag(DependencyObject obj)
    {
        return (bool)obj.GetValue(CanDragProperty);
    }

    public static void SetCanDrag(DependencyObject obj, bool value)
    {
        obj.SetValue(CanDragProperty, value);
    }

    public static readonly DependencyProperty CanDragProperty =
        DependencyProperty.RegisterAttached(
            "CanDrag",
            typeof(bool),
            typeof(DragWindowBehavior),
            new UIPropertyMetadata(false, OnCanDragChanged));

    private static void OnCanDragChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if ((bool)e.NewValue)
            {
                element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            }
            else
            {
                element.MouseLeftButtonDown -= Element_MouseLeftButtonDown;
            }
        }
    }

    private static void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement element)
        {
            var window = Window.GetWindow(element);
            if (window != null)
            {
                window.DragMove();
            }
        }
    }
}
