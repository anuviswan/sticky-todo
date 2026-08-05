using System.Text;
using System.Text.RegularExpressions;

namespace StickyDo.Domain.Models.RichText;

/// <summary>
/// Builds a word-truncated preview of formatted text (e.g. for a notes-list card) together with
/// formatting spans remapped onto that preview's own offsets - not the original text's. A preview
/// is built by re-joining a subset of words with single spaces, which does not preserve original
/// character offsets whenever the source has anything other than single-space whitespace (a
/// blank line between paragraphs, a tab, ...), so span offsets can't just be reused/clipped as-is
/// the way the full-text FlowDocument path does; each span has to be sliced against and
/// translated onto each word it overlaps.
/// </summary>
public static class RichTextPreviewBuilder
{
    public static (string PreviewText, RichTextFormatting? PreviewFormatting) BuildPreview(string? content, RichTextFormatting? formatting, int maxWords)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (string.Empty, null);

        var words = Regex.Matches(content, @"\S+");
        var takenCount = Math.Min(words.Count, maxWords);

        var previewBuilder = new StringBuilder();
        var wordOutputStarts = new int[takenCount];
        for (var i = 0; i < takenCount; i++)
        {
            if (i > 0)
                previewBuilder.Append(' ');
            wordOutputStarts[i] = previewBuilder.Length;
            previewBuilder.Append(words[i].Value);
        }

        if (words.Count > maxWords)
            previewBuilder.Append("...");

        var previewText = previewBuilder.ToString();

        if (formatting is null || formatting.Spans.Count == 0)
            return (previewText, null);

        var remappedSpans = new List<RichTextSpan>();
        for (var i = 0; i < takenCount; i++)
        {
            var word = words[i];
            var wordStart = word.Index;
            var wordEnd = word.Index + word.Length;
            var outputStart = wordOutputStarts[i];

            foreach (var span in formatting.Spans)
            {
                var overlapStart = Math.Max(span.Start, wordStart);
                var overlapEnd = Math.Min(span.Start + span.Length, wordEnd);
                if (overlapEnd <= overlapStart)
                    continue;

                remappedSpans.Add(new RichTextSpan
                {
                    Start = outputStart + (overlapStart - wordStart),
                    Length = overlapEnd - overlapStart,
                    Bold = span.Bold,
                    Italic = span.Italic,
                    Underline = span.Underline,
                    Strikethrough = span.Strikethrough
                });
            }
        }

        var previewFormatting = remappedSpans.Count > 0 ? new RichTextFormatting { Spans = remappedSpans } : null;
        return (previewText, previewFormatting);
    }
}
