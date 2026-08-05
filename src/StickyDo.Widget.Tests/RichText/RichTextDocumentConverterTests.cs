using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models.RichText;
using StickyDo.Widget.Controls.RichText;

namespace StickyDo.Widget.Tests.RichText;

[TestClass]
public class RichTextDocumentConverterTests
{
    [TestMethod]
    public void RoundTrip_PlainText_NoFormatting()
    {
        var document = RichTextDocumentConverter.BuildDocument("Buy milk and bread", null);

        var (plainText, formatting) = RichTextDocumentConverter.ToPlainTextAndFormatting(document);

        Assert.AreEqual("Buy milk and bread", plainText);
        Assert.IsNull(formatting);
    }

    [TestMethod]
    public void RoundTrip_SingleBoldSpan()
    {
        var original = new RichTextFormatting { Spans = [new RichTextSpan { Start = 4, Length = 4, Bold = true }] };
        var document = RichTextDocumentConverter.BuildDocument("Buy milk and bread", original);

        var (plainText, formatting) = RichTextDocumentConverter.ToPlainTextAndFormatting(document);

        Assert.AreEqual("Buy milk and bread", plainText);
        Assert.IsNotNull(formatting);
        Assert.AreEqual(1, formatting.Spans.Count);
        Assert.IsTrue(formatting.Spans[0] is { Start: 4, Length: 4, Bold: true, Italic: false, Underline: false, Strikethrough: false });
    }

    [TestMethod]
    public void RoundTrip_CombinedBoldItalicUnderlineStrikethrough_OnSameRange()
    {
        var original = new RichTextFormatting
        {
            Spans = [new RichTextSpan { Start = 0, Length = 5, Bold = true, Italic = true, Underline = true, Strikethrough = true }]
        };
        var document = RichTextDocumentConverter.BuildDocument("Hello world", original);

        var (plainText, formatting) = RichTextDocumentConverter.ToPlainTextAndFormatting(document);

        Assert.AreEqual("Hello world", plainText);
        Assert.IsNotNull(formatting);
        Assert.AreEqual(1, formatting.Spans.Count);
        var span = formatting.Spans[0];
        Assert.AreEqual(0, span.Start);
        Assert.AreEqual(5, span.Length);
        Assert.IsTrue(span.Bold);
        Assert.IsTrue(span.Italic);
        Assert.IsTrue(span.Underline);
        Assert.IsTrue(span.Strikethrough);
    }

    [TestMethod]
    public void RoundTrip_MultipleNonAdjacentRanges()
    {
        var original = new RichTextFormatting
        {
            Spans =
            [
                new RichTextSpan { Start = 0, Length = 3, Bold = true },
                new RichTextSpan { Start = 10, Length = 4, Italic = true }
            ]
        };
        var document = RichTextDocumentConverter.BuildDocument("Foo is a bar baz", original);

        var (plainText, formatting) = RichTextDocumentConverter.ToPlainTextAndFormatting(document);

        Assert.AreEqual("Foo is a bar baz", plainText);
        Assert.IsNotNull(formatting);
        Assert.AreEqual(2, formatting.Spans.Count);
        Assert.IsTrue(formatting.Spans.Any(s => s is { Start: 0, Length: 3, Bold: true }));
        Assert.IsTrue(formatting.Spans.Any(s => s is { Start: 10, Length: 4, Italic: true }));
    }

    [TestMethod]
    public void RoundTrip_MultiParagraphContent_PreservesLineBreaks()
    {
        // "Line 1\nBold\nLine 3" - "Bold" starts right after the first '\n', at index 7.
        var original = new RichTextFormatting { Spans = [new RichTextSpan { Start = 7, Length = 4, Bold = true }] };
        var document = RichTextDocumentConverter.BuildDocument("Line 1\nBold\nLine 3", original);

        var (plainText, formatting) = RichTextDocumentConverter.ToPlainTextAndFormatting(document);

        Assert.AreEqual("Line 1\nBold\nLine 3", plainText);
        Assert.IsNotNull(formatting);
        Assert.AreEqual(1, formatting.Spans.Count);
        Assert.IsTrue(formatting.Spans[0] is { Start: 7, Length: 4, Bold: true });
    }

    [TestMethod]
    public void RoundTrip_EmptyContent()
    {
        var document = RichTextDocumentConverter.BuildDocument(string.Empty, null);

        var (plainText, formatting) = RichTextDocumentConverter.ToPlainTextAndFormatting(document);

        Assert.AreEqual(string.Empty, plainText);
        Assert.IsNull(formatting);
    }

    [TestMethod]
    public void RoundTrip_NullPlainTextAndFormatting_TreatedAsEmpty()
    {
        var document = RichTextDocumentConverter.BuildDocument(null, null);

        var (plainText, formatting) = RichTextDocumentConverter.ToPlainTextAndFormatting(document);

        Assert.AreEqual(string.Empty, plainText);
        Assert.IsNull(formatting);
    }

    [TestMethod]
    public void BuildDocument_SpanOutsideTextBounds_IsClippedSafely()
    {
        // Defensive: a span referencing offsets beyond the current text (e.g. stale data from a
        // future edit that shortened the text) must not throw and must not fabricate characters.
        var formatting = new RichTextFormatting { Spans = [new RichTextSpan { Start = 2, Length = 100, Bold = true }] };

        var document = RichTextDocumentConverter.BuildDocument("Hi", formatting);
        var (plainText, resultFormatting) = RichTextDocumentConverter.ToPlainTextAndFormatting(document);

        Assert.AreEqual("Hi", plainText);
        Assert.IsNull(resultFormatting);
    }
}
