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

    /// <summary>Gets or sets the color brush for the note.</summary>
    public Brush ColorBrush
    {
        get => (Brush)GetValue(ColorBrushProperty);
        set => SetValue(ColorBrushProperty, value);
    }

    public static readonly DependencyProperty ColorBrushProperty =
        DependencyProperty.Register("ColorBrush", typeof(Brush), typeof(StickyNoteListItem),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 193, 7))));

    /// <summary>Gets or sets whether the note has at least one task.</summary>
    public bool HasTasks
    {
        get => (bool)GetValue(HasTasksProperty);
        set => SetValue(HasTasksProperty, value);
    }

    public static readonly DependencyProperty HasTasksProperty =
        DependencyProperty.Register("HasTasks", typeof(bool), typeof(StickyNoteListItem),
            new PropertyMetadata(false));

    /// <summary>Gets or sets the title of the note's first task.</summary>
    public string FirstTaskTitle
    {
        get => (string)GetValue(FirstTaskTitleProperty);
        set => SetValue(FirstTaskTitleProperty, value);
    }

    public static readonly DependencyProperty FirstTaskTitleProperty =
        DependencyProperty.Register("FirstTaskTitle", typeof(string), typeof(StickyNoteListItem),
            new PropertyMetadata(string.Empty));

    /// <summary>Gets or sets whether the note's first task is completed.</summary>
    public bool FirstTaskCompleted
    {
        get => (bool)GetValue(FirstTaskCompletedProperty);
        set => SetValue(FirstTaskCompletedProperty, value);
    }

    public static readonly DependencyProperty FirstTaskCompletedProperty =
        DependencyProperty.Register("FirstTaskCompleted", typeof(bool), typeof(StickyNoteListItem),
            new PropertyMetadata(false));

    /// <summary>Gets or sets the number of additional tasks beyond the first.</summary>
    public int RemainingTaskCount
    {
        get => (int)GetValue(RemainingTaskCountProperty);
        set => SetValue(RemainingTaskCountProperty, value);
    }

    public static readonly DependencyProperty RemainingTaskCountProperty =
        DependencyProperty.Register("RemainingTaskCount", typeof(int), typeof(StickyNoteListItem),
            new PropertyMetadata(0));
}
