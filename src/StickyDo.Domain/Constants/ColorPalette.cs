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
        0xFFFDD835,  // Amber - Warm, inviting (Default)
        0xFF4CAF50,  // Green - Fresh, calm
        0xFFE53935,  // Red - Urgent, important
        0xFF1E88E5,  // Blue - Professional, trustworthy
        0xFFE91E63,  // Pink - Creative, attention-grabbing
        0xFFFF9800,  // Orange - Energetic, warm
        0xFF9C27B0,  // Purple - Creative, imaginative
        0xFF00ACC1,  // Cyan - Clear, modern
        0xFFD32F2F,  // Deep Red - Critical, alert
        0xFF512DA8,  // Deep Purple - Formal, important
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
            0xFFFDD835 => "Amber",
            0xFF4CAF50 => "Green",
            0xFFE53935 => "Red",
            0xFF1E88E5 => "Blue",
            0xFFE91E63 => "Pink",
            0xFFFF9800 => "Orange",
            0xFF9C27B0 => "Purple",
            0xFF00ACC1 => "Cyan",
            0xFFD32F2F => "Deep Red",
            0xFF512DA8 => "Deep Purple",
            _ => "Unknown"
        };
    }
}
