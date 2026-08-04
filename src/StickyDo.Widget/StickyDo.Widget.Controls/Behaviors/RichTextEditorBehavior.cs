using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using StickyDo.Domain.Models.RichText;
using StickyDo.Widget.Controls.RichText;

namespace StickyDo.Widget.Controls.Behaviors;

/// <summary>
/// Turns a <see cref="RichTextBox"/> into an MVVM-friendly Bold/Italic/Underline/Strikethrough
/// editor: <see cref="PlainTextProperty"/> and <see cref="FormattingProperty"/> are TwoWay
/// attached properties that stay in sync with the box's <see cref="FlowDocument"/> via
/// <see cref="RichTextDocumentConverter"/>, without ever replacing the live Document on every
/// keystroke (that would destroy the native undo stack and caret position - the Document is only
/// (re)built from PlainText/Formatting on load or on an externally-driven change).
/// </summary>
/// <remarks>
/// Also tracks, per hosting <see cref="Window"/>, which managed <see cref="RichTextBox"/> last
/// had focus (<see cref="FocusedEditorProperty"/>) and its current Bold/Italic/Underline/
/// Strikethrough state (the IsXActive properties). A single footer toolbar binds to these
/// Window-level properties so its buttons always act on "whichever editor the cursor/selection
/// currently is in" - the Note's content box, or whichever Todo task row - the same way the
/// Windows 11 Sticky Notes formatting toolbar behaves, rather than being wired to one fixed
/// control.
/// </remarks>
public static class RichTextEditorBehavior
{
    #region PlainText (attached to RichTextBox, TwoWay)

    public static readonly DependencyProperty PlainTextProperty =
        DependencyProperty.RegisterAttached(
            "PlainText",
            typeof(string),
            typeof(RichTextEditorBehavior),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPlainTextOrFormattingChanged, CoerceEnsureInitialized));

    public static string GetPlainText(DependencyObject obj) => (string)obj.GetValue(PlainTextProperty);
    public static void SetPlainText(DependencyObject obj, string value) => obj.SetValue(PlainTextProperty, value);

    #endregion

    #region Formatting (attached to RichTextBox, TwoWay)

    public static readonly DependencyProperty FormattingProperty =
        DependencyProperty.RegisterAttached(
            "Formatting",
            typeof(RichTextFormatting),
            typeof(RichTextEditorBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPlainTextOrFormattingChanged, CoerceEnsureInitialized));

    public static RichTextFormatting? GetFormatting(DependencyObject obj) => (RichTextFormatting?)obj.GetValue(FormattingProperty);
    public static void SetFormatting(DependencyObject obj, RichTextFormatting? value) => obj.SetValue(FormattingProperty, value);

    #endregion

    #region SingleLine / SubmitCommand (attached to RichTextBox)

    /// <summary>When true, Enter invokes <see cref="SubmitCommandProperty"/> instead of inserting a new paragraph.</summary>
    public static readonly DependencyProperty SingleLineProperty =
        DependencyProperty.RegisterAttached("SingleLine", typeof(bool), typeof(RichTextEditorBehavior), new PropertyMetadata(false));

    public static bool GetSingleLine(DependencyObject obj) => (bool)obj.GetValue(SingleLineProperty);
    public static void SetSingleLine(DependencyObject obj, bool value) => obj.SetValue(SingleLineProperty, value);

    public static readonly DependencyProperty SubmitCommandProperty =
        DependencyProperty.RegisterAttached("SubmitCommand", typeof(ICommand), typeof(RichTextEditorBehavior), new PropertyMetadata(null));

    public static ICommand? GetSubmitCommand(DependencyObject obj) => (ICommand?)obj.GetValue(SubmitCommandProperty);
    public static void SetSubmitCommand(DependencyObject obj, ICommand? value) => obj.SetValue(SubmitCommandProperty, value);

    #endregion

    #region Window-level: FocusedEditor + IsXActive (read-only from XAML)

    private static readonly DependencyPropertyKey FocusedEditorPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly("FocusedEditor", typeof(RichTextBox), typeof(RichTextEditorBehavior), new PropertyMetadata(null));
    public static readonly DependencyProperty FocusedEditorProperty = FocusedEditorPropertyKey.DependencyProperty;
    public static RichTextBox? GetFocusedEditor(DependencyObject obj) => (RichTextBox?)obj.GetValue(FocusedEditorProperty);
    private static void SetFocusedEditor(DependencyObject obj, RichTextBox? value) => obj.SetValue(FocusedEditorPropertyKey, value);

    private static readonly DependencyPropertyKey IsBoldActivePropertyKey =
        DependencyProperty.RegisterAttachedReadOnly("IsBoldActive", typeof(bool), typeof(RichTextEditorBehavior), new PropertyMetadata(false));
    public static readonly DependencyProperty IsBoldActiveProperty = IsBoldActivePropertyKey.DependencyProperty;
    public static bool GetIsBoldActive(DependencyObject obj) => (bool)obj.GetValue(IsBoldActiveProperty);

    private static readonly DependencyPropertyKey IsItalicActivePropertyKey =
        DependencyProperty.RegisterAttachedReadOnly("IsItalicActive", typeof(bool), typeof(RichTextEditorBehavior), new PropertyMetadata(false));
    public static readonly DependencyProperty IsItalicActiveProperty = IsItalicActivePropertyKey.DependencyProperty;
    public static bool GetIsItalicActive(DependencyObject obj) => (bool)obj.GetValue(IsItalicActiveProperty);

    private static readonly DependencyPropertyKey IsUnderlineActivePropertyKey =
        DependencyProperty.RegisterAttachedReadOnly("IsUnderlineActive", typeof(bool), typeof(RichTextEditorBehavior), new PropertyMetadata(false));
    public static readonly DependencyProperty IsUnderlineActiveProperty = IsUnderlineActivePropertyKey.DependencyProperty;
    public static bool GetIsUnderlineActive(DependencyObject obj) => (bool)obj.GetValue(IsUnderlineActiveProperty);

    private static readonly DependencyPropertyKey IsStrikethroughActivePropertyKey =
        DependencyProperty.RegisterAttachedReadOnly("IsStrikethroughActive", typeof(bool), typeof(RichTextEditorBehavior), new PropertyMetadata(false));
    public static readonly DependencyProperty IsStrikethroughActiveProperty = IsStrikethroughActivePropertyKey.DependencyProperty;
    public static bool GetIsStrikethroughActive(DependencyObject obj) => (bool)obj.GetValue(IsStrikethroughActiveProperty);

    #endregion

    // Internal guard so pushing a converted value into PlainText/Formatting (from TextChanged)
    // doesn't loop back into rebuilding the Document those values were just derived from.
    private static readonly DependencyProperty IsSyncingFromDocumentProperty =
        DependencyProperty.RegisterAttached("IsSyncingFromDocument", typeof(bool), typeof(RichTextEditorBehavior), new PropertyMetadata(false));

    private static readonly DependencyProperty IsInitializedProperty =
        DependencyProperty.RegisterAttached("IsInitialized", typeof(bool), typeof(RichTextEditorBehavior), new PropertyMetadata(false));

    // Guards against subscribing richTextBox.Loaded more than once while waiting for it to fire.
    private static readonly DependencyProperty IsAwaitingLoadProperty =
        DependencyProperty.RegisterAttached("IsAwaitingLoad", typeof(bool), typeof(RichTextEditorBehavior), new PropertyMetadata(false));

    private static void OnPlainTextOrFormattingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox richTextBox)
            return;

        if (!(bool)richTextBox.GetValue(IsInitializedProperty))
            return; // CoerceEnsureInitialized (below) owns getting the box to its initial state.

        if ((bool)richTextBox.GetValue(IsSyncingFromDocumentProperty))
            return; // This change is us pushing the value we just read from the Document - ignore it.

        RebuildDocument(richTextBox);
    }

    /// <summary>
    /// Guarantees initialization gets scheduled the moment either property is first bound, by
    /// running as a CoerceValueCallback rather than off <see cref="OnPlainTextOrFormattingChanged"/>.
    /// WPF only invokes a PropertyChangedCallback when the *value* actually changes - but a
    /// brand-new note's Content is "", identical to PlainTextProperty's own default, so that
    /// callback would never fire for it at all. CoerceValueCallback, by contrast, runs on every
    /// SetValue (including the one a Binding performs as soon as it's applied), regardless of
    /// whether the resulting value differs from the previous one, so it can't be skipped this way.
    /// </summary>
    private static object? CoerceEnsureInitialized(DependencyObject d, object? baseValue)
    {
        if (d is RichTextBox richTextBox)
            EnsureInitialized(richTextBox);

        return baseValue;
    }

    private static void EnsureInitialized(RichTextBox richTextBox)
    {
        if ((bool)richTextBox.GetValue(IsInitializedProperty) || (bool)richTextBox.GetValue(IsAwaitingLoadProperty))
            return;

        if (richTextBox.IsLoaded)
        {
            InitializeRichTextBox(richTextBox);
        }
        else
        {
            richTextBox.SetValue(IsAwaitingLoadProperty, true);
            richTextBox.Loaded += OnRichTextBoxLoaded;
        }
    }

    private static void OnRichTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        var richTextBox = (RichTextBox)sender;
        richTextBox.Loaded -= OnRichTextBoxLoaded;
        richTextBox.SetValue(IsAwaitingLoadProperty, false);

        if (!(bool)richTextBox.GetValue(IsInitializedProperty))
            InitializeRichTextBox(richTextBox);
    }

    private static void InitializeRichTextBox(RichTextBox richTextBox)
    {
        richTextBox.SetValue(IsInitializedProperty, true);
        RebuildDocument(richTextBox);
        RichTextEditingCommands.RegisterCommandBinding(richTextBox);

        richTextBox.TextChanged += OnTextChanged;
        richTextBox.SelectionChanged += OnSelectionChanged;
        richTextBox.GotFocus += OnGotFocus;
        if (GetSingleLine(richTextBox))
            richTextBox.PreviewKeyDown += OnPreviewKeyDownSingleLine;
    }

    private static void RebuildDocument(RichTextBox richTextBox)
    {
        richTextBox.SetValue(IsSyncingFromDocumentProperty, true);
        try
        {
            richTextBox.Document = RichTextDocumentConverter.BuildDocument(GetPlainText(richTextBox), GetFormatting(richTextBox));
        }
        finally
        {
            richTextBox.SetValue(IsSyncingFromDocumentProperty, false);
        }
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var richTextBox = (RichTextBox)sender;
        var (plainText, formatting) = RichTextDocumentConverter.ToPlainTextAndFormatting(richTextBox.Document);

        richTextBox.SetValue(IsSyncingFromDocumentProperty, true);
        try
        {
            richTextBox.SetCurrentValue(PlainTextProperty, plainText);
            richTextBox.SetCurrentValue(FormattingProperty, formatting);
        }
        finally
        {
            richTextBox.SetValue(IsSyncingFromDocumentProperty, false);
        }
    }

    private static void OnSelectionChanged(object sender, RoutedEventArgs e) => UpdateActiveFormattingState((RichTextBox)sender);

    private static void OnGotFocus(object sender, RoutedEventArgs e)
    {
        var richTextBox = (RichTextBox)sender;
        if (Window.GetWindow(richTextBox) is { } window)
            SetFocusedEditor(window, richTextBox);

        UpdateActiveFormattingState(richTextBox);
    }

    private static void UpdateActiveFormattingState(RichTextBox richTextBox)
    {
        if (Window.GetWindow(richTextBox) is not { } window)
            return;

        var selection = richTextBox.Selection;
        var decorations = selection.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;

        window.SetValue(IsBoldActivePropertyKey, Equals(selection.GetPropertyValue(Inline.FontWeightProperty), FontWeights.Bold));
        window.SetValue(IsItalicActivePropertyKey, Equals(selection.GetPropertyValue(Inline.FontStyleProperty), FontStyles.Italic));
        window.SetValue(IsUnderlineActivePropertyKey, decorations is not null && decorations.Any(d => d.Location == TextDecorationLocation.Underline));
        window.SetValue(IsStrikethroughActivePropertyKey, decorations is not null && decorations.Any(d => d.Location == TextDecorationLocation.Strikethrough));
    }

    private static void OnPreviewKeyDownSingleLine(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        var richTextBox = (RichTextBox)sender;
        if (GetSubmitCommand(richTextBox) is { } command && command.CanExecute(null))
            command.Execute(null);
    }
}
