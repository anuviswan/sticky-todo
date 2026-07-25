using System.Windows;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Attached behavior that wires up sticky note window lifecycle handling: saves state when
/// the window moves, resizes, or is closed. Set behaviors:WindowBehavior.IsEnabled="True" on
/// the Window in XAML rather than attaching from code-behind.
/// </summary>
public static class WindowBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(WindowBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window || e.NewValue is not true)
            return;

        // Persist the window's position/size as it's moved or resized, so it survives even if
        // the application is later terminated without a clean shutdown (e.g. force-killed).
        window.LocationChanged += (s, args) =>
        {
            if (window.DataContext is StickyNoteWindowViewModel viewModel)
            {
                _ = viewModel.UpdateWindowBoundsAsync(window.Left, window.Top, window.Width, window.Height);
            }
        };

        window.SizeChanged += (s, args) =>
        {
            if (window.DataContext is StickyNoteWindowViewModel viewModel)
            {
                _ = viewModel.UpdateWindowBoundsAsync(window.Left, window.Top, window.Width, window.Height);
            }
        };

        // Wire up window closed to save state
        window.Closed += async (s, args) =>
        {
            if (window.DataContext is StickyNoteWindowViewModel viewModel)
            {
                await viewModel.SaveAsync();
            }
        };
    }
}
