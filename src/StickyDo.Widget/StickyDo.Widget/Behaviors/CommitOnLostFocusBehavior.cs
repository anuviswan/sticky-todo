using System.Windows;
using System.Windows.Input;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Attached behavior that invokes a bound command when the target element loses keyboard focus.
/// Used to commit a pending edit (e.g. the "Add a task" input) when the user clicks elsewhere,
/// tabs away, or the window is deactivated - not just on an explicit submit action like Enter.
/// LostKeyboardFocus (rather than the logical-focus LostFocus event) is used deliberately: it
/// also fires when the window loses activation entirely (e.g. the user clicks another app or the
/// desktop), which LostFocus does not.
/// </summary>
public static class CommitOnLostFocusBehavior
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(CommitOnLostFocusBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    public static ICommand? GetCommand(DependencyObject obj) => (ICommand?)obj.GetValue(CommandProperty);
    public static void SetCommand(DependencyObject obj, ICommand? value) => obj.SetValue(CommandProperty, value);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        element.LostKeyboardFocus -= Element_LostKeyboardFocus;

        if (e.NewValue is not null)
        {
            element.LostKeyboardFocus += Element_LostKeyboardFocus;
        }
    }

    private static void Element_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        if (GetCommand(element) is { } command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}
