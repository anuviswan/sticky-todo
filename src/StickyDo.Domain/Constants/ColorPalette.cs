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
        0xFF7EBAE8,  // Blue - Professional, trustworthy (Default)
        0xFFC1E8A3,  // Green - Fresh, calm
        0xFFC5E0F0,  // Light Blue - Serene, focused
        0xFFF5D8E8,  // Pink - Creative, attention-grabbing
        0xFFE0D8F0,  // Purple - Imaginative, thoughtful
        0xFFE8E8E8,  // Gray - Neutral, balanced
        0xFF5A5A5A,  // Dark Gray - Professional, formal
        0xFFC0E8E0,  // Cyan - Clear, modern
        0xFFF5D8B0,  // Orange - Energetic, warm
        0xFFF8D0C0,  // Salmon - Soft, warm
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
            0xFF7EBAE8 => "Blue",
            0xFFC1E8A3 => "Green",
            0xFFC5E0F0 => "Light Blue",
            0xFFF5D8E8 => "Pink",
            0xFFE0D8F0 => "Purple",
            0xFFE8E8E8 => "Gray",
            0xFF5A5A5A => "Dark Gray",
            0xFFC0E8E0 => "Cyan",
            0xFFF5D8B0 => "Orange",
            0xFFF8D0C0 => "Salmon",
            _ => "Unknown"
        };
    }
}
