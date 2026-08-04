using System.Text;
using System.Windows;
using System.Windows.Documents;
using StickyDo.Domain.Models.RichText;

namespace StickyDo.Widget.Controls.RichText;

/// <summary>
/// Converts between a WPF <see cref="FlowDocument"/> (used for in-place rich-text editing in a
/// <see cref="System.Windows.Controls.RichTextBox"/>) and the plain-text + <see cref="RichTextFormatting"/>
/// shape persisted to disk. Pure data conversion with no dependency on a live window or visual
/// tree, so both directions are directly unit-testable.
/// </summary>
public static class RichTextDocumentConverter
{
    /// <summary>
    /// Builds a <see cref="FlowDocument"/> from plain text and optional formatting spans. One
    /// <see cref="Paragraph"/> is created per '\n'-delimited line. Intended for initial load /
    /// externally-driven resets only - never call this from a live edit's TextChanged handler,
    /// since replacing the Document destroys the RichTextBox's caret position and undo stack.
    /// </summary>
    public static FlowDocument BuildDocument(string? plainText, RichTextFormatting? formatting)
    {
        var text = plainText ?? string.Empty;
        var document = new FlowDocument();

        var lineStart = 0;
        foreach (var line in text.Split('\n'))
        {
            var paragraph = new Paragraph();
            AppendLineRuns(paragraph, line, lineStart, formatting);
            document.Blocks.Add(paragraph);
            lineStart += line.Length + 1; // +1 accounts for the '\n' separator consumed by Split
        }

        return document;
    }

    private static void AppendLineRuns(Paragraph paragraph, string line, int lineStart, RichTextFormatting? formatting)
    {
        if (line.Length == 0)
            return;

        var flagsPerChar = new SpanFlags[line.Length];
        if (formatting is not null)
        {
            foreach (var span in formatting.Spans)
            {
                var overlapStart = Math.Max(Math.Max(span.Start, lineStart) - lineStart, 0);
                var overlapEnd = Math.Min(span.Start + span.Length - lineStart, line.Length);
                for (var i = overlapStart; i < overlapEnd; i++)
                {
                    flagsPerChar[i].Bold |= span.Bold;
                    flagsPerChar[i].Italic |= span.Italic;
                    flagsPerChar[i].Underline |= span.Underline;
                    flagsPerChar[i].Strikethrough |= span.Strikethrough;
                }
            }
        }

        var runStart = 0;
        for (var i = 1; i <= line.Length; i++)
        {
            if (i == line.Length || !flagsPerChar[i].Equals(flagsPerChar[runStart]))
            {
                paragraph.Inlines.Add(CreateRun(line.Substring(runStart, i - runStart), flagsPerChar[runStart]));
                runStart = i;
            }
        }
    }

    private static Run CreateRun(string text, SpanFlags flags)
    {
        var run = new Run(text);

        if (flags.Bold)
            run.FontWeight = FontWeights.Bold;
        if (flags.Italic)
            run.FontStyle = FontStyles.Italic;

        var decorations = new TextDecorationCollection();
        if (flags.Underline)
            decorations.Add(TextDecorations.Underline[0]);
        if (flags.Strikethrough)
            decorations.Add(TextDecorations.Strikethrough[0]);
        if (decorations.Count > 0)
            run.TextDecorations = decorations;

        return run;
    }

    /// <summary>
    /// Walks a <see cref="FlowDocument"/> and rebuilds the plain text + formatting spans it
    /// represents, from scratch. Only understands the flat Paragraph/Run structure this class
    /// itself produces (and that the Bold/Italic/Underline/Strikethrough editing commands mutate
    /// in place) - other inline content (e.g. from a rich paste) contributes its plain text with
    /// no formatting, which is the desired behavior since this feature doesn't support any
    /// formatting beyond those four styles.
    /// </summary>
    public static (string PlainText, RichTextFormatting? Formatting) ToPlainTextAndFormatting(FlowDocument document)
    {
        var textBuilder = new StringBuilder();
        var spans = new List<RichTextSpan>();

        var isFirstParagraph = true;
        foreach (var block in document.Blocks)
        {
            if (!isFirstParagraph)
                textBuilder.Append('\n');
            isFirstParagraph = false;

            if (block is Paragraph paragraph)
            {
                foreach (var inline in paragraph.Inlines)
                    AppendInlineText(inline, textBuilder, spans);
            }
        }

        var plainText = textBuilder.ToString();
        var formatting = spans.Count > 0 ? new RichTextFormatting { Spans = spans } : null;
        return (plainText, formatting);
    }

    private static void AppendInlineText(Inline inline, StringBuilder textBuilder, List<RichTextSpan> spans)
    {
        switch (inline)
        {
            case Run run:
                AppendRunText(run, textBuilder, spans);
                break;
            case LineBreak:
                textBuilder.Append('\n');
                break;
            case Span span:
                foreach (var child in span.Inlines)
                    AppendInlineText(child, textBuilder, spans);
                break;
            default:
                textBuilder.Append(new TextRange(inline.ContentStart, inline.ContentEnd).Text);
                break;
        }
    }

    private static void AppendRunText(Run run, StringBuilder textBuilder, List<RichTextSpan> spans)
    {
        var text = run.Text;
        if (string.IsNullOrEmpty(text))
            return;

        var start = textBuilder.Length;
        textBuilder.Append(text);

        var isBold = run.FontWeight == FontWeights.Bold;
        var isItalic = run.FontStyle == FontStyles.Italic;
        var decorations = run.TextDecorations;
        var isUnderline = decorations is not null && decorations.Any(d => d.Location == TextDecorationLocation.Underline);
        var isStrikethrough = decorations is not null && decorations.Any(d => d.Location == TextDecorationLocation.Strikethrough);

        if (isBold || isItalic || isUnderline || isStrikethrough)
        {
            spans.Add(new RichTextSpan
            {
                Start = start,
                Length = text.Length,
                Bold = isBold,
                Italic = isItalic,
                Underline = isUnderline,
                Strikethrough = isStrikethrough
            });
        }
    }

    private struct SpanFlags : IEquatable<SpanFlags>
    {
        public bool Bold;
        public bool Italic;
        public bool Underline;
        public bool Strikethrough;

        public bool Equals(SpanFlags other) =>
            Bold == other.Bold && Italic == other.Italic && Underline == other.Underline && Strikethrough == other.Strikethrough;

        public override bool Equals(object? obj) => obj is SpanFlags other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Bold, Italic, Underline, Strikethrough);
    }
}
