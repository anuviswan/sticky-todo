using System.Reflection;
using System.Resources;

namespace StickyDo.Widget.Resources;

/// <summary>
/// Typed accessor for the localizable UI strings in Resources.resx. Add a culture-specific
/// Resources.&lt;culture&gt;.resx alongside this file to localize; the ResourceManager picks it
/// up automatically based on the current UI culture.
/// </summary>
public static class Resources
{
    private static readonly ResourceManager ResourceManager =
        new("StickyDo.Widget.Resources.Resources", Assembly.GetExecutingAssembly());

    public static string MoreOptionsPopup_NotesList => Get(nameof(MoreOptionsPopup_NotesList));
    public static string MoreOptionsPopup_DeleteNote => Get(nameof(MoreOptionsPopup_DeleteNote));
    public static string DeleteNote_ConfirmTitle => Get(nameof(DeleteNote_ConfirmTitle));
    public static string DeleteNote_ConfirmMessage => Get(nameof(DeleteNote_ConfirmMessage));
    public static string DragDeleteNote_ConfirmTitle => Get(nameof(DragDeleteNote_ConfirmTitle));
    public static string DragDeleteNote_ConfirmMessage => Get(nameof(DragDeleteNote_ConfirmMessage));
    public static string DragDeleteNote_ErrorTitle => Get(nameof(DragDeleteNote_ErrorTitle));
    public static string DragDeleteNote_ErrorMessage => Get(nameof(DragDeleteNote_ErrorMessage));
    public static string Favorite_ErrorTitle => Get(nameof(Favorite_ErrorTitle));
    public static string Favorite_ErrorMessage => Get(nameof(Favorite_ErrorMessage));
    public static string SyncingStatus => Get(nameof(SyncingStatus));
    public static string SyncedStatus => Get(nameof(SyncedStatus));
    public static string ErrorStatus => Get(nameof(ErrorStatus));
    public static string JustNow => Get(nameof(JustNow));
    public static string ZeroNotes => Get(nameof(ZeroNotes));
    public static string ErrorLoadingNotes => Get(nameof(ErrorLoadingNotes));
    public static string LoadErrorTitle => Get(nameof(LoadErrorTitle));
    public static string SaveStatus_Saved => Get(nameof(SaveStatus_Saved));
    public static string SaveStatus_Saving => Get(nameof(SaveStatus_Saving));
    public static string SaveStatus_NotSaved => Get(nameof(SaveStatus_NotSaved));
    public static string SyncStatus_NotSynced => Get(nameof(SyncStatus_NotSynced));

    private static string Get(string name) => ResourceManager.GetString(name) ?? name;
}
