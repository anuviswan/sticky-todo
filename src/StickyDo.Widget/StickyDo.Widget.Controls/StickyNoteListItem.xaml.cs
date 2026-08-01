using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StickyDo.Widget.Controls;

/// <summary>
/// Reusable WPF user control for displaying a sticky note item in a list.
/// </summary>
public partial class StickyNoteListItem : UserControl
{
    /// <summary>Drag-and-drop data format key used to carry a note's Guid when dragged (e.g. onto the Trash icon).</summary>
    public const string NoteIdDataFormat = "StickyDo.NoteId";

    public StickyNoteListItem()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets the unique identifier of the note, passed to <see cref="ToggleFavoriteCommand"/>.</summary>
    public Guid NoteId
    {
        get => (Guid)GetValue(NoteIdProperty);
        set => SetValue(NoteIdProperty, value);
    }

    public static readonly DependencyProperty NoteIdProperty =
        DependencyProperty.Register("NoteId", typeof(Guid), typeof(StickyNoteListItem),
            new PropertyMetadata(Guid.Empty));

    /// <summary>Gets or sets whether the note is marked as a favourite.</summary>
    public bool IsFavorite
    {
        get => (bool)GetValue(IsFavoriteProperty);
        set => SetValue(IsFavoriteProperty, value);
    }

    public static readonly DependencyProperty IsFavoriteProperty =
        DependencyProperty.Register("IsFavorite", typeof(bool), typeof(StickyNoteListItem),
            new PropertyMetadata(false));

    /// <summary>Gets or sets the command invoked with <see cref="NoteId"/> when the favourite icon is clicked.</summary>
    public ICommand? ToggleFavoriteCommand
    {
        get => (ICommand?)GetValue(ToggleFavoriteCommandProperty);
        set => SetValue(ToggleFavoriteCommandProperty, value);
    }

    public static readonly DependencyProperty ToggleFavoriteCommandProperty =
        DependencyProperty.Register("ToggleFavoriteCommand", typeof(ICommand), typeof(StickyNoteListItem),
            new PropertyMetadata(null));

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

    /// <summary>Gets or sets the truncated preview of a free-form note's content.</summary>
    public string ContentPreview
    {
        get => (string)GetValue(ContentPreviewProperty);
        set => SetValue(ContentPreviewProperty, value);
    }

    public static readonly DependencyProperty ContentPreviewProperty =
        DependencyProperty.Register("ContentPreview", typeof(string), typeof(StickyNoteListItem),
            new PropertyMetadata(string.Empty));

    /// <summary>Gets or sets the number of additional tasks beyond the first.</summary>
    public int RemainingTaskCount
    {
        get => (int)GetValue(RemainingTaskCountProperty);
        set => SetValue(RemainingTaskCountProperty, value);
    }

    public static readonly DependencyProperty RemainingTaskCountProperty =
        DependencyProperty.Register("RemainingTaskCount", typeof(int), typeof(StickyNoteListItem),
            new PropertyMetadata(0));

    /// <summary>Gets or sets the note's type (a boxed <c>NoteType</c> enum value), selecting the
    /// Todo/Note icon shown next to the title. Boxed as <see cref="object"/>, compared by string
    /// representation in XAML, so this assembly doesn't need to reference the Domain project.</summary>
    public object? Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public static readonly DependencyProperty TypeProperty =
        DependencyProperty.Register("Type", typeof(object), typeof(StickyNoteListItem),
            new PropertyMetadata(null));
}
