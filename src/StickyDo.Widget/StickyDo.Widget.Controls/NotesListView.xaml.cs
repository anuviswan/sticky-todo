using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace StickyDo.Widget.Controls;

/// <summary>
/// Reusable control for displaying a list of sticky notes with empty state handling.
/// </summary>
public partial class NotesListView : UserControl
{
    public NotesListView()
    {
        InitializeComponent();
        Loaded += (s, e) => UpdateEmptyState();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property.Name == "DataContext")
        {
            UpdateEmptyState();
        }
    }

    private void UpdateEmptyState()
    {
        if (DataContext == null)
            return;

        try
        {
            var vmType = DataContext.GetType();
            var filteredNotesProperty = vmType.GetProperty("FilteredNotes");
            if (filteredNotesProperty != null)
            {
                var filteredNotes = filteredNotesProperty.GetValue(DataContext) as ICollection;
                if (filteredNotes != null)
                {
                    var hasNotes = filteredNotes.Count > 0;
                    NotesScrollViewer.Visibility = hasNotes ? Visibility.Visible : Visibility.Collapsed;
                    EmptyStatePanel.Visibility = hasNotes ? Visibility.Collapsed : Visibility.Visible;

                    if (filteredNotes is INotifyCollectionChanged notifyCollectionChanged)
                    {
                        notifyCollectionChanged.CollectionChanged += (s, e) => UpdateEmptyState();
                    }
                }
            }
        }
        catch
        {
            // Handle any binding errors gracefully
        }
    }
}
