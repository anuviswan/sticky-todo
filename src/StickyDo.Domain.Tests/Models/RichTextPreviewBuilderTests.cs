using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models.RichText;

namespace StickyDo.Domain.Tests.Models;

[TestClass]
public class RichTextPreviewBuilderTests
{
    [TestMethod]
    public void BuildPreview_NullOrWhitespaceContent_ReturnsEmpty()
    {
        var (text, formatting) = RichTextPreviewBuilder.BuildPreview(null, null, 12);

        Assert.AreEqual(string.Empty, text);
        Assert.IsNull(formatting);
    }

    [TestMethod]
    public void BuildPreview_FitsWithinMaxWords_NoTruncationMarker()
    {
        var (text, formatting) = RichTextPreviewBuilder.BuildPreview("one two three", null, 12);

        Assert.AreEqual("one two three", text);
        Assert.IsNull(formatting);
    }

    [TestMethod]
    public void BuildPreview_ExceedsMaxWords_TruncatesWithEllipsis()
    {
        var (text, _) = RichTextPreviewBuilder.BuildPreview("one two three four five", null, 3);

        Assert.AreEqual("one two three...", text);
    }

    [TestMethod]
    public void BuildPreview_NoFormatting_ReturnsNullFormatting()
    {
        var (_, formatting) = RichTextPreviewBuilder.BuildPreview("plain text here", null, 12);

        Assert.IsNull(formatting);
    }

    [TestMethod]
    public void BuildPreview_SpanWithinFirstWord_RemapsToSameOffsetWhenNoLeadingWhitespaceShift()
    {
        // "Hello world" - "Hello" (0-4) is bold. Preview text is identical to source here, so the
        // remapped span should land at the exact same offsets.
        var formatting = new RichTextFormatting { Spans = [new RichTextSpan { Start = 0, Length = 5, Bold = true }] };

        var (text, preview) = RichTextPreviewBuilder.BuildPreview("Hello world", formatting, 12);

        Assert.AreEqual("Hello world", text);
        Assert.IsNotNull(preview);
        Assert.AreEqual(1, preview.Spans.Count);
        Assert.IsTrue(preview.Spans[0] is { Start: 0, Length: 5, Bold: true });
    }

    [TestMethod]
    public void BuildPreview_MultipleSpacesBetweenWords_RemapsOffsetsNotJustClips()
    {
        // "Foo    Bar" has 4 spaces between words (source offsets: Foo=0-3, Bar=7-10). The preview
        // re-joins with a single space ("Foo Bar", Bar now at offset 4) - if offsets were reused
        // as-is instead of remapped, "Bar" would incorrectly land on the space character.
        var formatting = new RichTextFormatting { Spans = [new RichTextSpan { Start = 7, Length = 3, Bold = true }] };

        var (text, preview) = RichTextPreviewBuilder.BuildPreview("Foo    Bar", formatting, 12);

        Assert.AreEqual("Foo Bar", text);
        Assert.IsNotNull(preview);
        Assert.AreEqual(1, preview.Spans.Count);
        Assert.IsTrue(preview.Spans[0] is { Start: 4, Length: 3, Bold: true });
        Assert.AreEqual("Bar", text.Substring(preview.Spans[0].Start, preview.Spans[0].Length));
    }

    [TestMethod]
    public void BuildPreview_NewlineBetweenWords_RemapsOffsetsCorrectly()
    {
        // "Line one\nLine two" - a blank-line-separated note. "two" (source offset 14-17) should
        // land correctly in the single-line, space-joined preview.
        var content = "Line one\nLine two";
        var formatting = new RichTextFormatting { Spans = [new RichTextSpan { Start = 14, Length = 3, Italic = true }] };

        var (text, preview) = RichTextPreviewBuilder.BuildPreview(content, formatting, 12);

        Assert.AreEqual("Line one Line two", text);
        Assert.IsNotNull(preview);
        Assert.AreEqual(1, preview.Spans.Count);
        Assert.AreEqual("two", text.Substring(preview.Spans[0].Start, preview.Spans[0].Length));
        Assert.IsTrue(preview.Spans[0].Italic);
    }

    [TestMethod]
    public void BuildPreview_SpanInDroppedWord_IsExcluded()
    {
        var content = "one two three four five six seven eight nine ten eleven twelve thirteen";
        // "thirteen" is word #13, beyond a 12-word cap - its span must not appear in the preview.
        var thirteenStart = content.IndexOf("thirteen", StringComparison.Ordinal);
        var formatting = new RichTextFormatting { Spans = [new RichTextSpan { Start = thirteenStart, Length = 8, Bold = true }] };

        var (text, preview) = RichTextPreviewBuilder.BuildPreview(content, formatting, 12);

        Assert.IsTrue(text.EndsWith("..."));
        Assert.IsFalse(text.Contains("thirteen"));
        Assert.IsNull(preview);
    }

    [TestMethod]
    public void BuildPreview_SpanSplitAcrossTwoWords_RemapsBothPortionsIndependently()
    {
        // A single span covering "foo bar" (space included) spans two words; each word's overlap
        // portion should be remapped to its own new position in the preview.
        var content = "foo bar baz";
        var formatting = new RichTextFormatting { Spans = [new RichTextSpan { Start = 0, Length = 7, Underline = true }] }; // "foo bar"

        var (text, preview) = RichTextPreviewBuilder.BuildPreview(content, formatting, 12);

        Assert.AreEqual("foo bar baz", text);
        Assert.IsNotNull(preview);
        Assert.AreEqual(2, preview.Spans.Count);
        Assert.IsTrue(preview.Spans.Any(s => s is { Start: 0, Length: 3, Underline: true })); // "foo"
        Assert.IsTrue(preview.Spans.Any(s => s is { Start: 4, Length: 3, Underline: true })); // "bar"
    }
}
