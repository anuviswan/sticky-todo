using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickyDo.Domain.Constants;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for the Settings page shown within the main window's content area. Holds only
/// local, in-memory UI state for the controls whose real behavior (Windows startup
/// registration, default-color persistence, import/export, update checks, settings
/// persistence) is delivered by separate tickets; this view model exists to give the page
/// something to bind to, not to implement that behavior.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool launchAtStartup;

    [ObservableProperty]
    private uint selectedDefaultColor = ColorPalette.GetDefaultColor();

    [ObservableProperty]
    private ObservableCollection<uint> availableColors = new(ColorPalette.Colors);

    [ObservableProperty]
    private string applicationName = Resources.Resources.Settings_ApplicationName;

    [ObservableProperty]
    private string applicationVersion = GetApplicationVersion();

    [ObservableProperty]
    private string copyrightText = string.Format(Resources.Resources.Settings_Copyright, DateTime.Now.Year);

    /// <summary>
    /// Raised when the user requests to leave the Settings page (e.g. via its close button).
    /// The hosting view model swaps the content area back to the notes list, keeping this
    /// ViewModel view-agnostic.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Selects the default note color. Local UI state only - does not persist or affect
    /// newly created notes.
    /// </summary>
    [RelayCommand]
    public void SelectDefaultColor(uint color)
    {
        SelectedDefaultColor = color;
    }

    /// <summary>
    /// Requests that the Settings page close and control return to the notes list.
    /// </summary>
    [RelayCommand]
    public void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private static string GetApplicationVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? string.Empty : $"Version {version.ToString(3)}";
    }
}
