using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using StickyDo.Widget.Controls;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Views;

/// <summary>
/// View for displaying a list of sticky notes with dedicated ViewModel.
/// DataContext is bound via XAML binding to parent MainWindowViewModel.NotesListViewModel.
/// </summary>
public partial class NotesListView : UserControl
{
    private Point _dragStartPoint;
    private bool _isDragging;
    private Popup? _dragPreviewPopup;

    public NotesListView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Opens a note as a floating sticky note window when double-clicked in the list.
    /// </summary>
    private void StickyNoteListItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2)
            return;

        if (sender is FrameworkElement { DataContext: StickyNoteItemViewModel note } &&
            DataContext is NotesListViewModel viewModel)
        {
            _ = viewModel.OpenNoteAsync(note.Id);
        }
    }

    /// <summary>
    /// Records the mouse-down position so a subsequent move past the drag threshold can start a
    /// drag operation, e.g. to drop the note onto the Trash icon.
    /// </summary>
    private void StickyNoteListItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    /// <summary>
    /// Starts a drag-and-drop operation once the mouse moves past the system drag threshold with
    /// the left button held. Dims the card, switches the cursor to a hand, and shows a small
    /// floating "note" chip that follows the pointer, so it's unmistakable that a note (and not
    /// just the mouse) is being moved. All of it is restored/torn down afterward regardless of
    /// whether the drop was accepted.
    /// </summary>
    private void StickyNoteListItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging || e.LeftButton != MouseButtonState.Pressed)
            return;

        if (sender is not FrameworkElement { DataContext: StickyNoteItemViewModel note } element)
            return;

        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _isDragging = true;
        var originalOpacity = element.Opacity;
        element.Opacity = 0.5;

        _dragPreviewPopup = CreateDragPreviewPopup(note.Title);
        _dragPreviewPopup.IsOpen = true;
        MoveDragPreviewToCursor();
        element.GiveFeedback += OnGiveFeedback;

        try
        {
            var data = new DataObject(StickyNoteListItem.NoteIdDataFormat, note.Id);
            DragDrop.DoDragDrop(element, data, DragDropEffects.Move);
        }
        finally
        {
            element.GiveFeedback -= OnGiveFeedback;
            _dragPreviewPopup.IsOpen = false;
            _dragPreviewPopup = null;
            element.Opacity = originalOpacity;
            _isDragging = false;
        }
    }

    /// <summary>
    /// Fires continuously on the drag source while a drag is in progress. WPF resets the cursor
    /// to its default on every tick unless a handler opts out, so both the cursor override and
    /// the floating preview's position have to be reasserted here rather than set once up front.
    /// </summary>
    private void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
        e.UseDefaultCursors = false;
        Mouse.SetCursor(Cursors.Hand);
        MoveDragPreviewToCursor();
        e.Handled = true;
    }

    /// <summary>
    /// Positions the floating drag-preview popup just below/right of the current cursor location,
    /// converting from raw screen pixels to WPF's device-independent units so it lines up
    /// correctly at any display scaling.
    /// </summary>
    private void MoveDragPreviewToCursor()
    {
        if (_dragPreviewPopup is null)
            return;

        var screenPosition = System.Windows.Forms.Cursor.Position;
        var source = PresentationSource.FromVisual(this);
        var position = source?.CompositionTarget is null
            ? new Point(screenPosition.X, screenPosition.Y)
            : source.CompositionTarget.TransformFromDevice.Transform(new Point(screenPosition.X, screenPosition.Y));

        _dragPreviewPopup.HorizontalOffset = position.X + 18;
        _dragPreviewPopup.VerticalOffset = position.Y + 18;
    }

    /// <summary>
    /// Builds the small floating chip shown next to the cursor while dragging a note, identifying
    /// which note is being moved.
    /// </summary>
    private static Popup CreateDragPreviewPopup(string noteTitle)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new TextBlock
        {
            Text = "📝",
            FontSize = 14,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = noteTitle,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 160,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var chip = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6, 10, 6),
            Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 2, Opacity = 0.25 },
            Child = panel
        };

        return new Popup
        {
            Child = chip,
            Placement = PlacementMode.Absolute,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.None,
            IsHitTestVisible = false,
            Focusable = false
        };
    }
}
