namespace StickyDo.Domain.Models.RichText;

/// <summary>
/// A formatted range over an associated plain-text string, identified by character offset.
/// </summary>
public sealed class RichTextSpan
{
    /// <summary>Zero-based character offset where the span starts.</summary>
    public int Start { get; set; }

    /// <summary>Number of characters covered by the span.</summary>
    public int Length { get; set; }

    public bool Bold { get; set; }

    public bool Italic { get; set; }

    public bool Underline { get; set; }

    public bool Strikethrough { get; set; }
}
