using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace StickyDo.Widget.Controls.Behaviors;

/// <summary>
/// Custom editing commands not provided by WPF's built-in <see cref="EditingCommands"/>.
/// </summary>
public static class RichTextEditingCommands
{
    /// <summary>
    /// Toggles strikethrough on the current selection (or, for a collapsed caret, the format
    /// that will apply to subsequently typed text) - the same semantics as the built-in
    /// <see cref="EditingCommands.ToggleUnderline"/>. Attach via <see cref="RichTextEditorBehavior"/>,
    /// which registers the required <see cref="System.Windows.Input.CommandBinding"/> on each
    /// <see cref="RichTextBox"/> it manages.
    /// </summary>
    public static readonly RoutedUICommand ToggleStrikethrough = new(
        "Toggle Strikethrough", nameof(ToggleStrikethrough), typeof(RichTextEditingCommands));

    internal static void RegisterCommandBinding(RichTextBox richTextBox)
    {
        richTextBox.CommandBindings.Add(new CommandBinding(ToggleStrikethrough, Execute, CanExecute));
    }

    private static void CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = sender is RichTextBox { IsReadOnly: false };
    }

    private static void Execute(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is not RichTextBox richTextBox)
            return;

        var selection = richTextBox.Selection;
        var current = selection.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
        var hasStrikethrough = current is not null && current.Any(d => d.Location == TextDecorationLocation.Strikethrough);

        // Preserve any other decoration already present (namely Underline) instead of replacing
        // the whole collection, so Bold+Italic+Underline+Strikethrough can all be combined.
        var updated = new TextDecorationCollection(current ?? new TextDecorationCollection());
        if (hasStrikethrough)
            updated = new TextDecorationCollection(updated.Where(d => d.Location != TextDecorationLocation.Strikethrough));
        else
            updated.Add(TextDecorations.Strikethrough[0]);

        // BeginChange/EndChange groups this into a single UndoManager unit, exactly like WPF's
        // built-in ToggleBold/ToggleItalic/ToggleUnderline commands - without it, this manual
        // property mutation either doesn't participate in Undo at all or fragments into several
        // separate undo steps.
        richTextBox.BeginChange();
        try
        {
            selection.ApplyPropertyValue(Inline.TextDecorationsProperty, updated.Count > 0 ? updated : null);
        }
        finally
        {
            richTextBox.EndChange();
        }
    }
}
