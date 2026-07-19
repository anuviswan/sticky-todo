using System.Globalization;
using System.Windows.Data;

namespace StickyDo.Widget.Controls;

/// <summary>
/// Converts full note content to a preview string (first 60 characters or first line).
/// </summary>
public class ContentPreviewConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        var content = value as string;
        if (string.IsNullOrEmpty(content))
            return "(empty)";

        var lines = content.Split('\n');
        var preview = lines[0].Length > 60
            ? lines[0].Substring(0, 60) + "…"
            : lines[0];

        return string.IsNullOrEmpty(preview) && lines.Length > 1
            ? lines[1].Length > 60 ? lines[1].Substring(0, 60) + "…" : lines[1]
            : preview;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
