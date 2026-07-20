using System.Windows;
using System.Windows.Controls;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Attached behavior for programmatically focusing a UIElement from the ViewModel.
/// Bind IsFocused to a ViewModel boolean property.
/// </summary>
public static class FocusBehavior
{
    public static bool GetIsFocused(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsFocusedProperty);
    }

    public static void SetIsFocused(DependencyObject obj, bool value)
    {
        obj.SetValue(IsFocusedProperty, value);
    }

    public static readonly DependencyProperty IsFocusedProperty =
        DependencyProperty.RegisterAttached(
            "IsFocused",
            typeof(bool),
            typeof(FocusBehavior),
            new UIPropertyMetadata(false, OnIsFocusedChanged));

    private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue && d is UIElement element)
        {
            element.Focus();
        }
    }
}
