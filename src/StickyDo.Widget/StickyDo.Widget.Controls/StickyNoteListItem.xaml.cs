using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StickyDo.Widget.Controls;

/// <summary>
/// Reusable WPF user control for displaying a sticky note item in a list.
/// </summary>
public partial class StickyNoteListItem : UserControl
{
    private static readonly ContentPreviewConverter _previewConverter = new();
    private static readonly StatusToBrushConverter _statusConverter = new();
    private static readonly LastModifiedConverter _lastModifiedConverter = new();

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
            control.ContentPreview = (string)_previewConverter.Convert(content, typeof(string), null, null);
        }
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StickyNoteListItem control)
        {
            var status = (string)e.NewValue ?? "Active";
            control.StatusBackground = (Brush)_statusConverter.Convert(status, typeof(Brush), null, null);
        }
    }

    private static void OnLastModifiedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StickyNoteListItem control)
        {
            var dateTime = (DateTime)e.NewValue;
            control.LastModifiedDisplay = (string)_lastModifiedConverter.Convert(dateTime, typeof(string), null, null);
        }
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
