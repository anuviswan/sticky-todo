using System.Windows;
using System.Windows.Input;

namespace StickyDo.Widget.Controls.Behaviors;

/// <summary>
/// Attached behavior that turns any UIElement into an MVVM-friendly drop target: set DataFormat
/// to the DataObject format key expected (e.g. StickyNoteListItem.NoteIdDataFormat) and
/// DropCommand to a bound ICommand&lt;T&gt; that receives the dropped value as its parameter.
/// WPF's DragDrop API has no commanding surface of its own, so this is the single place that
/// bridges it to the ViewModel instead of each drop target needing its own code-behind handlers.
/// IsDragOver is toggled internally and is read-only for callers; bind a Style Trigger to it for
/// drop-target visual feedback.
/// </summary>
public static class DragDropTargetBehavior
{
    public static ICommand? GetDropCommand(DependencyObject obj) => (ICommand?)obj.GetValue(DropCommandProperty);

    public static void SetDropCommand(DependencyObject obj, ICommand? value) => obj.SetValue(DropCommandProperty, value);

    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.RegisterAttached(
            "DropCommand",
            typeof(ICommand),
            typeof(DragDropTargetBehavior),
            new PropertyMetadata(null, OnDropCommandChanged));

    public static string? GetDataFormat(DependencyObject obj) => (string?)obj.GetValue(DataFormatProperty);

    public static void SetDataFormat(DependencyObject obj, string? value) => obj.SetValue(DataFormatProperty, value);

    public static readonly DependencyProperty DataFormatProperty =
        DependencyProperty.RegisterAttached(
            "DataFormat",
            typeof(string),
            typeof(DragDropTargetBehavior),
            new PropertyMetadata(null));

    public static bool GetIsDragOver(DependencyObject obj) => (bool)obj.GetValue(IsDragOverProperty);

    private static void SetIsDragOver(DependencyObject obj, bool value) => obj.SetValue(IsDragOverPropertyKey, value);

    private static readonly DependencyPropertyKey IsDragOverPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "IsDragOver",
            typeof(bool),
            typeof(DragDropTargetBehavior),
            new UIPropertyMetadata(false));

    public static readonly DependencyProperty IsDragOverProperty = IsDragOverPropertyKey.DependencyProperty;

    private static void OnDropCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;

        if (e.OldValue is not null)
        {
            element.DragEnter -= OnDragOver;
            element.DragOver -= OnDragOver;
            element.DragLeave -= OnDragLeave;
            element.Drop -= OnDrop;
        }

        if (e.NewValue is not null)
        {
            element.AllowDrop = true;
            element.DragEnter += OnDragOver;
            element.DragOver += OnDragOver;
            element.DragLeave += OnDragLeave;
            element.Drop += OnDrop;
        }
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        var element = (UIElement)sender;
        var accepted = TryGetDroppedValue(element, e, out _);

        e.Effects = accepted ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
        SetIsDragOver((DependencyObject)sender, accepted);
    }

    private static void OnDragLeave(object sender, DragEventArgs e)
    {
        SetIsDragOver((DependencyObject)sender, false);
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        var element = (UIElement)sender;
        SetIsDragOver((DependencyObject)sender, false);

        if (!TryGetDroppedValue(element, e, out var value))
            return;

        var command = GetDropCommand((DependencyObject)sender);
        if (command?.CanExecute(value) == true)
            command.Execute(value);
    }

    private static bool TryGetDroppedValue(UIElement element, DragEventArgs e, out object? value)
    {
        value = null;
        var format = GetDataFormat(element);
        if (string.IsNullOrEmpty(format) || !e.Data.GetDataPresent(format))
            return false;

        value = e.Data.GetData(format);
        return value is not null;
    }
}
