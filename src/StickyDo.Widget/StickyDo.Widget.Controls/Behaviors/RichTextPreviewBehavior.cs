using System.Windows;
using System.Windows.Controls;
using StickyDo.Domain.Models.RichText;
using StickyDo.Widget.Controls.RichText;

namespace StickyDo.Widget.Controls.Behaviors;

/// <summary>
/// Renders Bold/Italic/Underline/Strikethrough formatting into a read-only <see cref="TextBlock"/>'s
/// Inlines - for surfaces that display formatted text without editing it, e.g. a notes-list card
/// preview. Much simpler than <see cref="RichTextEditorBehavior"/>: one-directional (no TwoWay
/// binding, no undo, no selection/focus tracking), and building Inlines doesn't depend on the
/// element being part of a rendered visual tree, so there's no Loaded-timing concern either - a
/// plain PropertyChangedCallback is sufficient here.
/// </summary>
public static class RichTextPreviewBehavior
{
    public static readonly DependencyProperty PlainTextProperty =
        DependencyProperty.RegisterAttached("PlainText", typeof(string), typeof(RichTextPreviewBehavior),
            new PropertyMetadata(string.Empty, OnChanged));

    public static string GetPlainText(DependencyObject obj) => (string)obj.GetValue(PlainTextProperty);
    public static void SetPlainText(DependencyObject obj, string value) => obj.SetValue(PlainTextProperty, value);

    public static readonly DependencyProperty FormattingProperty =
        DependencyProperty.RegisterAttached("Formatting", typeof(RichTextFormatting), typeof(RichTextPreviewBehavior),
            new PropertyMetadata(null, OnChanged));

    public static RichTextFormatting? GetFormatting(DependencyObject obj) => (RichTextFormatting?)obj.GetValue(FormattingProperty);
    public static void SetFormatting(DependencyObject obj, RichTextFormatting? value) => obj.SetValue(FormattingProperty, value);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            return;

        textBlock.Inlines.Clear();
        foreach (var run in RichTextDocumentConverter.BuildRuns(GetPlainText(textBlock), GetFormatting(textBlock)))
            textBlock.Inlines.Add(run);
    }
}
