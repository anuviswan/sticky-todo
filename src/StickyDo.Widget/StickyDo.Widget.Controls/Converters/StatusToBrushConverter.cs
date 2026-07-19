using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StickyDo.Widget.Controls;

/// <summary>
/// Converts note status string to a background brush color.
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        var status = value as string ?? "Active";
        return status switch
        {
            "Active" => new SolidColorBrush(Color.FromRgb(244, 167, 23)),      // Amber
            "Completed" => new SolidColorBrush(Color.FromRgb(0, 179, 111)),    // Green
            "Archived" => new SolidColorBrush(Color.FromRgb(158, 158, 158)),   // Gray
            "Urgent" => new SolidColorBrush(Color.FromRgb(229, 57, 53)),       // Red
            _ => new SolidColorBrush(Color.FromRgb(0, 179, 111))
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
