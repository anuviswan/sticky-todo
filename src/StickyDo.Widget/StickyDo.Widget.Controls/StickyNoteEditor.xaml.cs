using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StickyDo.Widget.Controls;

/// <summary>
/// Reusable WPF user control for creating and editing sticky notes.
/// </summary>
public partial class StickyNoteEditor : UserControl
{
    public event EventHandler? SaveClicked;
    public event EventHandler? CancelClicked;

    public StickyNoteEditor()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets the title of the note being edited.</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(StickyNoteEditor),
            new PropertyMetadata(string.Empty));

    /// <summary>Gets or sets the content of the note being edited.</summary>
    public string NoteContent
    {
        get => (string)GetValue(NoteContentProperty);
        set => SetValue(NoteContentProperty, value);
    }

    public static readonly DependencyProperty NoteContentProperty =
        DependencyProperty.Register("NoteContent", typeof(string), typeof(StickyNoteEditor),
            new PropertyMetadata(string.Empty, OnNoteContentChanged));

    /// <summary>Gets or sets the status of the note.</summary>
    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register("Status", typeof(string), typeof(StickyNoteEditor),
            new PropertyMetadata("Active"));

    /// <summary>Gets the character count of the content.</summary>
    public int CharacterCount
    {
        get => (int)GetValue(CharacterCountProperty);
        private set => SetValue(CharacterCountProperty, value);
    }

    public static readonly DependencyProperty CharacterCountProperty =
        DependencyProperty.Register("CharacterCount", typeof(int), typeof(StickyNoteEditor),
            new PropertyMetadata(0));

    /// <summary>Gets or sets the color brush for the note.</summary>
    public Brush ColorBrush
    {
        get => (Brush)GetValue(ColorBrushProperty);
        set => SetValue(ColorBrushProperty, value);
    }

    public static readonly DependencyProperty ColorBrushProperty =
        DependencyProperty.Register("ColorBrush", typeof(Brush), typeof(StickyNoteEditor),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 193, 7))));

    private static void OnNoteContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StickyNoteEditor control)
        {
            var content = (string)e.NewValue ?? string.Empty;
            control.CharacterCount = content.Length;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            MessageBox.Show("Please enter a title for the note.", "Required Field",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TitleTextBox?.Focus();
            return;
        }

        SaveClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        CancelClicked?.Invoke(this, EventArgs.Empty);
    }
}
