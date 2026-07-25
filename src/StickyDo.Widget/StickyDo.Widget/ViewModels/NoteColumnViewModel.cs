using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for a single color-grouped column of sticky notes in the notes list.
/// </summary>
public partial class NoteColumnViewModel : ObservableObject
{
    [ObservableProperty]
    private uint colorArgb;

    public ObservableCollection<StickyNoteItemViewModel> Notes { get; } = new();
}
