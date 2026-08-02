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
    public static string MoreOptionsPopup_ConvertToNote => Get(nameof(MoreOptionsPopup_ConvertToNote));
    public static string MoreOptionsPopup_ConvertToTodo => Get(nameof(MoreOptionsPopup_ConvertToTodo));
    public static string DeleteNote_ConfirmTitle => Get(nameof(DeleteNote_ConfirmTitle));
    public static string DeleteNote_ConfirmMessage => Get(nameof(DeleteNote_ConfirmMessage));
    public static string DragDeleteNote_ConfirmTitle => Get(nameof(DragDeleteNote_ConfirmTitle));
    public static string DragDeleteNote_ConfirmMessage => Get(nameof(DragDeleteNote_ConfirmMessage));
    public static string DragDeleteNote_ErrorTitle => Get(nameof(DragDeleteNote_ErrorTitle));
    public static string DragDeleteNote_ErrorMessage => Get(nameof(DragDeleteNote_ErrorMessage));
    public static string Favorite_ErrorTitle => Get(nameof(Favorite_ErrorTitle));
    public static string Favorite_ErrorMessage => Get(nameof(Favorite_ErrorMessage));
    public static string Search_ErrorTitle => Get(nameof(Search_ErrorTitle));
    public static string Search_ErrorMessage => Get(nameof(Search_ErrorMessage));
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
    public static string Settings_Title => Get(nameof(Settings_Title));
    public static string Settings_Subtitle => Get(nameof(Settings_Subtitle));
    public static string Settings_Section_General => Get(nameof(Settings_Section_General));
    public static string Settings_Section_DataManagement => Get(nameof(Settings_Section_DataManagement));
    public static string Settings_LaunchAtStartup_Title => Get(nameof(Settings_LaunchAtStartup_Title));
    public static string Settings_LaunchAtStartup_Description => Get(nameof(Settings_LaunchAtStartup_Description));
    public static string Settings_DefaultNoteColor_Title => Get(nameof(Settings_DefaultNoteColor_Title));
    public static string Settings_DefaultNoteColor_Description => Get(nameof(Settings_DefaultNoteColor_Description));
    public static string Settings_ImportNotes_Title => Get(nameof(Settings_ImportNotes_Title));
    public static string Settings_ImportNotes_Description => Get(nameof(Settings_ImportNotes_Description));
    public static string Settings_ExportNotes_Title => Get(nameof(Settings_ExportNotes_Title));
    public static string Settings_ExportNotes_Description => Get(nameof(Settings_ExportNotes_Description));
    public static string Settings_ApplicationName => Get(nameof(Settings_ApplicationName));
    public static string Settings_CheckForUpdates => Get(nameof(Settings_CheckForUpdates));
    public static string Settings_Copyright => Get(nameof(Settings_Copyright));
    public static string Settings_PrivacyPolicy => Get(nameof(Settings_PrivacyPolicy));
    public static string Settings_TermsOfService => Get(nameof(Settings_TermsOfService));

    private static string Get(string name) => ResourceManager.GetString(name) ?? name;
}
