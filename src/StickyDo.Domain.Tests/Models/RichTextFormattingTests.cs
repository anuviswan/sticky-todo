using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models.RichText;
using StickyDo.Domain.Serialization;

namespace StickyDo.Domain.Tests.Models;

[TestClass]
public class RichTextFormattingTests
{
    [TestMethod]
    public void Default_HasCurrentVersionAndEmptySpans()
    {
        var formatting = new RichTextFormatting();

        Assert.AreEqual(RichTextFormatting.CurrentVersion, formatting.Version);
        Assert.AreEqual(0, formatting.Spans.Count);
    }

    [TestMethod]
    public void RoundTrips_ThroughDomainJsonOptions()
    {
        var formatting = new RichTextFormatting
        {
            Spans =
            [
                new RichTextSpan { Start = 0, Length = 5, Bold = true, Italic = true },
                new RichTextSpan { Start = 10, Length = 3, Strikethrough = true }
            ]
        };

        var json = JsonSerializer.Serialize(formatting, JsonSerializationOptions.Default);
        var deserialized = JsonSerializer.Deserialize<RichTextFormatting>(json, JsonSerializationOptions.Default);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(formatting.Version, deserialized.Version);
        Assert.AreEqual(2, deserialized.Spans.Count);
        Assert.IsTrue(deserialized.Spans.Any(s => s is { Start: 0, Length: 5, Bold: true, Italic: true }));
        Assert.IsTrue(deserialized.Spans.Any(s => s is { Start: 10, Length: 3, Strikethrough: true }));
    }

    [TestMethod]
    public void Deserialize_EmptySpansArray_ProducesEmptyList()
    {
        var json = """{ "Version": 1, "Spans": [] }""";

        var deserialized = JsonSerializer.Deserialize<RichTextFormatting>(json, JsonSerializationOptions.Default);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(0, deserialized.Spans.Count);
    }
}
