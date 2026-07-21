namespace StickyDo.Domain.Constants;

/// <summary>
/// Color palette for sticky notes. Contains 10 carefully selected colors
/// that work well together and provide good visual distinction.
/// Based on Material Design color principles.
/// </summary>
public static class ColorPalette
{
    /// <summary>
    /// 10 ARGB color values optimized for sticky note applications.
    /// Colors are selected for visual distinctiveness, accessibility, and aesthetic appeal.
    /// </summary>
    public static readonly uint[] Colors =
    [
        0xFFfff4ad,  // Yellow - Warm, inviting (Default)
        0xFFd4f8d4,  // Green - Fresh, calm
        0xFFe2f1ff,  // Light Blue - Serene, focused
        0xFFf2d5f2,  // Pink - Creative, attention-grabbing
        0xFFe3d3fd,  // Purple - Imaginative, thoughtful
        0xFFf1f1f1,  // Gray - Neutral, balanced
        0xFF4c4c4c,  // Dark Gray - Professional, formal
        0xFFc1f3f1,  // Cyan - Clear, modern
        0xFFffdfb8,  // Orange - Energetic, warm
        0xFFffcfcf,  // Red - Alert, important
    ];

    /// <summary>
    /// Gets the default color (first color in palette).
    /// </summary>
    public static uint GetDefaultColor() => Colors[0];

    /// <summary>
    /// Gets a human-readable name for the color at the specified index.
    /// Useful for tooltips and accessibility features.
    /// </summary>
    public static string GetColorName(uint color)
    {
        return color switch
        {
            0xFFfff4ad => "Yellow",
            0xFFd4f8d4 => "Green",
            0xFFe2f1ff => "Light Blue",
            0xFFf2d5f2 => "Pink",
            0xFFe3d3fd => "Purple",
            0xFFf1f1f1 => "Gray",
            0xFF4c4c4c => "Dark Gray",
            0xFFc1f3f1 => "Cyan",
            0xFFffdfb8 => "Orange",
            0xFFffcfcf => "Red",
            _ => "Unknown"
        };
    }
}
