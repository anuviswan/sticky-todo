namespace StickyDo.Domain.Utilities;

/// <summary>
/// Utility class for color conversions and manipulations.
/// Handles RGB ↔ HSL conversions and color variants.
/// </summary>
public static class ColorUtility
{
    /// <summary>
    /// Represents a color in HSL format.
    /// </summary>
    public struct HslColor
    {
        public double Hue { get; set; }        // 0-360
        public double Saturation { get; set; } // 0-1
        public double Lightness { get; set; }  // 0-1
        public byte Alpha { get; set; }        // 0-255
    }

    /// <summary>
    /// Converts ARGB color to HSL format.
    /// </summary>
    public static HslColor ArgbToHsl(uint argb)
    {
        byte a = (byte)((argb >> 24) & 0xFF);
        byte r = (byte)((argb >> 16) & 0xFF);
        byte g = (byte)((argb >> 8) & 0xFF);
        byte b = (byte)(argb & 0xFF);

        double rn = r / 255.0;
        double gn = g / 255.0;
        double bn = b / 255.0;

        double max = Math.Max(rn, Math.Max(gn, bn));
        double min = Math.Min(rn, Math.Min(gn, bn));
        double l = (max + min) / 2.0;

        double h = 0;
        double s = 0;

        if (max != min)
        {
            double d = max - min;
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

            if (max == rn)
                h = (gn - bn) / d + (gn < bn ? 6 : 0);
            else if (max == gn)
                h = (bn - rn) / d + 2;
            else
                h = (rn - gn) / d + 4;

            h /= 6;
        }

        return new HslColor
        {
            Hue = h * 360,
            Saturation = s,
            Lightness = l,
            Alpha = a
        };
    }

    /// <summary>
    /// Converts HSL color to ARGB format.
    /// </summary>
    public static uint HslToArgb(HslColor hsl)
    {
        double h = hsl.Hue / 360.0;
        double s = hsl.Saturation;
        double l = hsl.Lightness;

        double r, g, b;

        if (s == 0)
        {
            r = g = b = l;
        }
        else
        {
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            r = HueToRgb(p, q, h + 1 / 3.0);
            g = HueToRgb(p, q, h);
            b = HueToRgb(p, q, h - 1 / 3.0);
        }

        byte rb = (byte)Math.Round(r * 255);
        byte gb = (byte)Math.Round(g * 255);
        byte bb = (byte)Math.Round(b * 255);

        return (uint)((hsl.Alpha << 24) | (rb << 16) | (gb << 8) | bb);
    }

    /// <summary>
    /// Calculates footer color variant based on selected color.
    /// Formula: Footer = (Hue=Unchanged, Saturation x0.85, Lightness x 0.85)
    /// </summary>
    public static uint GetFooterColor(uint selectedColor)
    {
        var hsl = ArgbToHsl(selectedColor);

        hsl.Saturation = Math.Min(1.0, hsl.Saturation * 0.85);
        hsl.Lightness = Math.Min(1.0, hsl.Lightness * 0.85);

        return HslToArgb(hsl);
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1 / 6.0) return p + (q - p) * 6 * t;
        if (t < 1 / 2.0) return q;
        if (t < 2 / 3.0) return p + (q - p) * (2 / 3.0 - t) * 6;
        return p;
    }
}
