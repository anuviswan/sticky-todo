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
        0xFF5B9FD1,  // Blue - Professional, trustworthy (Default)
        0xFFA4D65E,  // Green - Fresh, calm
        0xFFC5E1E8,  // Light Blue - Serene, focused
        0xFFF6D5E8,  // Pink - Creative, attention-grabbing
        0xFFE8D5F2,  // Purple - Imaginative, thoughtful
        0xFFE0E0E0,  // Gray - Neutral, balanced
        0xFF4A4A4A,  // Dark Gray - Professional, formal
        0xFFC5E8E0,  // Cyan - Clear, modern
        0xFFF6D8A0,  // Orange - Energetic, warm
        0xFFF6D0C0,  // Salmon - Soft, warm
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
            0xFF5B9FD1 => "Blue",
            0xFFA4D65E => "Green",
            0xFFC5E1E8 => "Light Blue",
            0xFFF6D5E8 => "Pink",
            0xFFE8D5F2 => "Purple",
            0xFFE0E0E0 => "Gray",
            0xFF4A4A4A => "Dark Gray",
            0xFFC5E8E0 => "Cyan",
            0xFFF6D8A0 => "Orange",
            0xFFF6D0C0 => "Salmon",
            _ => "Unknown"
        };
    }
}
