using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// Dedicated ViewModel for the "more options" popup (Notes List / Delete Note) on a sticky
/// note window. Owned and composed by StickyNoteWindowViewModel, which wires its callbacks.
/// </summary>
public partial class MoreOptionsPopupViewModel : ObservableObject
{
    private Func<Task>? _onDeleteNote;
    private Func<Task>? _onShowNotesList;

    public void SetCallbacks(Func<Task> onDeleteNote, Func<Task> onShowNotesList)
    {
        _onDeleteNote = onDeleteNote;
        _onShowNotesList = onShowNotesList;
    }

    /// <summary>
    /// Deletes the note (handled by the parent window ViewModel).
    /// </summary>
    [RelayCommand]
    public async Task DeleteNoteAsync()
    {
        if (_onDeleteNote != null)
        {
            await _onDeleteNote();
        }
    }

    /// <summary>
    /// Shows the notes list window (handled by the parent window ViewModel).
    /// </summary>
    [RelayCommand]
    public async Task ShowNotesListAsync()
    {
        if (_onShowNotesList != null)
        {
            await _onShowNotesList();
        }
    }
}
