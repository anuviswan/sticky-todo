using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Attached behavior that dismisses a Popup - via a bound command - when Escape is pressed or
/// the user clicks outside both the popup and its toggle button while the popup is open.
/// Set on the Popup element itself; DismissCommand and ToggleButton are resolved once the
/// popup's placement target has loaded into its owning window.
/// </summary>
public static class PopupDismissBehavior
{
    public static readonly DependencyProperty DismissCommandProperty =
        DependencyProperty.RegisterAttached(
            "DismissCommand",
            typeof(ICommand),
            typeof(PopupDismissBehavior),
            new PropertyMetadata(null, OnDismissCommandChanged));

    public static ICommand? GetDismissCommand(DependencyObject obj) => (ICommand?)obj.GetValue(DismissCommandProperty);
    public static void SetDismissCommand(DependencyObject obj, ICommand? value) => obj.SetValue(DismissCommandProperty, value);

    public static readonly DependencyProperty ToggleButtonProperty =
        DependencyProperty.RegisterAttached(
            "ToggleButton",
            typeof(UIElement),
            typeof(PopupDismissBehavior),
            new PropertyMetadata(null));

    public static UIElement? GetToggleButton(DependencyObject obj) => (UIElement?)obj.GetValue(ToggleButtonProperty);
    public static void SetToggleButton(DependencyObject obj, UIElement? value) => obj.SetValue(ToggleButtonProperty, value);

    private static void OnDismissCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Popup popup || e.NewValue is null)
            return;

        if (popup.PlacementTarget is DependencyObject placementTarget && Window.GetWindow(placementTarget) is { } window)
        {
            AttachToWindow(popup, window);
        }
        else
        {
            popup.Loaded += Popup_Loaded;
        }
    }

    private static void Popup_Loaded(object sender, RoutedEventArgs e)
    {
        var popup = (Popup)sender;
        popup.Loaded -= Popup_Loaded;

        if (popup.PlacementTarget is DependencyObject placementTarget && Window.GetWindow(placementTarget) is { } window)
        {
            AttachToWindow(popup, window);
        }
    }

    private static void AttachToWindow(Popup popup, Window window)
    {
        window.PreviewKeyDown += (s, args) =>
        {
            if (popup.IsOpen && args.Key == Key.Escape)
            {
                args.Handled = true;
                GetDismissCommand(popup)?.Execute(null);
            }
        };

        window.PreviewMouseDown += (s, args) =>
        {
            if (!popup.IsOpen)
                return;

            var hit = VisualTreeHelper.HitTest(window, args.GetPosition(window));
            if (hit?.VisualHit is null)
                return;

            var insidePopup = popup.Child is not null && IsDescendantOf(hit.VisualHit, popup.Child);
            var toggleButton = GetToggleButton(popup);
            var onToggleButton = toggleButton is not null && IsDescendantOf(hit.VisualHit, toggleButton);

            if (!insidePopup && !onToggleButton)
            {
                GetDismissCommand(popup)?.Execute(null);
            }
        };
    }

    private static bool IsDescendantOf(DependencyObject child, DependencyObject parent)
    {
        var current = child;
        while (current is not null)
        {
            if (current == parent)
                return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }
}
