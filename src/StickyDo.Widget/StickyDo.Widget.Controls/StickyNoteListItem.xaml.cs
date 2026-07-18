using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StickyDo.Widget.Controls;

/// <summary>
/// Reusable WPF user control for displaying a sticky note item in a list.
/// </summary>
public partial class StickyNoteListItem : UserControl
{
    public StickyNoteListItem()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets the title of the note.</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(StickyNoteListItem),
            new PropertyMetadata(string.Empty));

    /// <summary>Gets or sets the full content of the note.</summary>
    public string NoteContent
    {
        get => (string)GetValue(NoteContentProperty);
        set => SetValue(NoteContentProperty, value);
    }

    public static readonly DependencyProperty NoteContentProperty =
        DependencyProperty.Register("NoteContent", typeof(string), typeof(StickyNoteListItem),
            new PropertyMetadata(string.Empty, OnNoteContentChanged));

    /// <summary>Gets the preview text (first 60 characters) of the content.</summary>
    public string ContentPreview
    {
        get => (string)GetValue(ContentPreviewProperty);
        private set => SetValue(ContentPreviewProperty, value);
    }

    public static readonly DependencyProperty ContentPreviewProperty =
        DependencyProperty.Register("ContentPreview", typeof(string), typeof(StickyNoteListItem),
            new PropertyMetadata(string.Empty));

    /// <summary>Gets or sets the status of the note.</summary>
    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register("Status", typeof(string), typeof(StickyNoteListItem),
            new PropertyMetadata("Active", OnStatusChanged));

    /// <summary>Gets the background color for the status badge.</summary>
    public Brush StatusBackground
    {
        get => (Brush)GetValue(StatusBackgroundProperty);
        private set => SetValue(StatusBackgroundProperty, value);
    }

    public static readonly DependencyProperty StatusBackgroundProperty =
        DependencyProperty.Register("StatusBackground", typeof(Brush), typeof(StickyNoteListItem),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 179, 111))));

    /// <summary>Gets or sets the last modified date/time.</summary>
    public DateTime LastModified
    {
        get => (DateTime)GetValue(LastModifiedProperty);
        set => SetValue(LastModifiedProperty, value);
    }

    public static readonly DependencyProperty LastModifiedProperty =
        DependencyProperty.Register("LastModified", typeof(DateTime), typeof(StickyNoteListItem),
            new PropertyMetadata(DateTime.UtcNow, OnLastModifiedChanged));

    /// <summary>Gets the display text for last modified time.</summary>
    public string LastModifiedDisplay
    {
        get => (string)GetValue(LastModifiedDisplayProperty);
        private set => SetValue(LastModifiedDisplayProperty, value);
    }

    public static readonly DependencyProperty LastModifiedDisplayProperty =
        DependencyProperty.Register("LastModifiedDisplay", typeof(string), typeof(StickyNoteListItem),
            new PropertyMetadata("Just now"));

    /// <summary>Gets or sets the color brush for the note.</summary>
    public Brush ColorBrush
    {
        get => (Brush)GetValue(ColorBrushProperty);
        set => SetValue(ColorBrushProperty, value);
    }

    public static readonly DependencyProperty ColorBrushProperty =
        DependencyProperty.Register("ColorBrush", typeof(Brush), typeof(StickyNoteListItem),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 193, 7))));

    private static void OnNoteContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StickyNoteListItem control)
        {
            var content = (string)e.NewValue ?? string.Empty;
            control.ContentPreview = GeneratePreview(content);
        }
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StickyNoteListItem control)
        {
            var status = (string)e.NewValue ?? "Active";
            control.StatusBackground = GetStatusBackground(status);
        }
    }

    private static void OnLastModifiedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StickyNoteListItem control)
        {
            var dateTime = (DateTime)e.NewValue;
            control.LastModifiedDisplay = FormatLastModified(dateTime);
        }
    }

    private static string GeneratePreview(string content)
    {
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

    private static Brush GetStatusBackground(string status) => status switch
    {
        "Active" => new SolidColorBrush(Color.FromRgb(244, 167, 23)),      // Amber
        "Completed" => new SolidColorBrush(Color.FromRgb(0, 179, 111)),    // Green
        "Archived" => new SolidColorBrush(Color.FromRgb(158, 158, 158)),   // Gray
        "Urgent" => new SolidColorBrush(Color.FromRgb(229, 57, 53)),       // Red
        _ => new SolidColorBrush(Color.FromRgb(0, 179, 111))
    };

    private static string FormatLastModified(DateTime dateTime)
    {
        var elapsed = DateTime.UtcNow - dateTime;

        return elapsed.TotalSeconds < 60
            ? "Just now"
            : elapsed.TotalMinutes < 60
                ? $"{(int)elapsed.TotalMinutes}m ago"
                : elapsed.TotalHours < 24
                    ? $"{(int)elapsed.TotalHours}h ago"
                    : $"{(int)elapsed.TotalDays}d ago";
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Future: Add hover effects here
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Future: Remove hover effects here
    }
}
