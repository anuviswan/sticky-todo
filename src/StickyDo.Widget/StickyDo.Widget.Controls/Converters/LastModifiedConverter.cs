using System.Globalization;
using System.Windows.Data;

namespace StickyDo.Widget.Controls;

/// <summary>
/// Converts a DateTime to a human-readable relative time string (e.g., "5m ago", "2h ago").
/// </summary>
public class LastModifiedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is not DateTime dateTime)
            return "Just now";

        var elapsed = DateTime.UtcNow - dateTime;

        return elapsed.TotalSeconds < 60
            ? "Just now"
            : elapsed.TotalMinutes < 60
                ? $"{(int)elapsed.TotalMinutes}m ago"
                : elapsed.TotalHours < 24
                    ? $"{(int)elapsed.TotalHours}h ago"
                    : $"{(int)elapsed.TotalDays}d ago";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
