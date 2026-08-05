namespace StickyDo.Widget.Tests.RichText;

/// <summary>
/// Compiled (x:Class + InitializeComponent) copy of the placeholder + RichTextBox fragment from
/// StickyNoteWindow.xaml's Note-content area, used by StickyNoteContentEditorRegressionTests.
/// Genuinely compiled XAML matters here (as opposed to XamlReader.Parse at runtime): it's the
/// only way to faithfully exercise the exact code path StickyNoteWindow.xaml itself goes through.
/// </summary>
public partial class StickyNoteContentEditorProbe : System.Windows.Controls.Grid
{
    public StickyNoteContentEditorProbe()
    {
        InitializeComponent();
    }
}
