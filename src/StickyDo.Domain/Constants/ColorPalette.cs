namespace StickyDo.Domain.Constants;

public static class ColorPalette
{
    public static readonly uint[] Colors =
    [
        0xFFFFCC07,  // Yellow
        0xFF00B36F,  // Green
        0xFFE53935,  // Red
        0xFF0066CC,  // Blue
        0xFFC2185B,  // Pink
        0xFFFF9800,  // Orange
        0xFF9C27B0,  // Purple
        0xFF009688,  // Teal
        0xFFF57C00,  // Deep Orange
        0xFF455A64,  // Blue Grey
    ];

    public static uint GetDefaultColor() => Colors[0];
}
