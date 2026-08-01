using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickyDo.Domain.Models;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// Dedicated ViewModel for the "more options" popup (Notes List / Convert / Delete Note) on a
/// sticky note window. Owned and composed by StickyNoteWindowViewModel, which wires its
/// callbacks and keeps <see cref="Type"/> in sync with the note being edited.
/// </summary>
public partial class MoreOptionsPopupViewModel : ObservableObject
{
    private Func<Task>? _onDeleteNote;
    private Func<Task>? _onShowNotesList;
    private Func<Task>? _onConvertType;

    /// <summary>The current note's type, used to label the "Convert to ..." menu item.</summary>
    [ObservableProperty]
    private NoteType type = NoteType.Todo;

    public void SetCallbacks(Func<Task> onDeleteNote, Func<Task> onShowNotesList, Func<Task> onConvertType)
    {
        _onDeleteNote = onDeleteNote;
        _onShowNotesList = onShowNotesList;
        _onConvertType = onConvertType;
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

    /// <summary>
    /// Converts the note between Todo and Note (handled by the parent window ViewModel).
    /// </summary>
    [RelayCommand]
    public async Task ConvertTypeAsync()
    {
        if (_onConvertType != null)
        {
            await _onConvertType();
        }
    }
}
