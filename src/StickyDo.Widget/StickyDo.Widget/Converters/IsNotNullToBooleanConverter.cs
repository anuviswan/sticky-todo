using System.Globalization;
using System.Windows.Data;

namespace StickyDo.Widget.Converters;

/// <summary>
/// Converts any reference to a boolean: true when non-null. Used to enable the footer's rich-text
/// formatting buttons only once a content/task RichTextBox has been focused in this note window
/// (see <see cref="StickyDo.Widget.Controls.Behaviors.RichTextEditorBehavior.FocusedEditorProperty"/>).
/// </summary>
public class IsNotNullToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
