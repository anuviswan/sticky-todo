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

    public void SetCallbacks(Func<Task> onDeleteNote)
    {
        _onDeleteNote = onDeleteNote;
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
}
