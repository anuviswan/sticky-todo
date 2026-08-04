using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StickyDo.Domain.Constants;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Domain.Storage;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Messages;
using StickyDo.Widget.Utilities;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for the Settings page shown within the main window's content area.
/// <see cref="LaunchAtStartup"/> and <see cref="SelectedDefaultColor"/> are automatically
/// persisted via <see cref="ISettingsRepository"/> on every change. <see cref="SelectedDefaultColor"/>
/// is applied to every newly created note (see <c>NotesListViewModel.CreateNoteAsync</c>); the real
/// behavior behind <see cref="LaunchAtStartup"/> (Windows startup registration) and update checks
/// is delivered by separate tickets.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private const string PrivacyPolicyUrl = "https://github.com/anuviswan/sticky-todo/blob/main/PRIVACY_POLICY.md";
    private const string TermsOfServiceUrl = "https://github.com/anuviswan/sticky-todo/blob/main/TERMS_OF_SERVICE.md";

    private readonly ISettingsRepository _settingsRepository;
    private readonly IBackupService _backupService;
    private readonly IFilePickerService _filePickerService;
    private readonly IDialogService _dialogService;
    private readonly IStorageLocationProvider _storageLocationProvider;
    private readonly FileBasedRepository _noteRepository;
    private readonly IMessenger _messenger;
    private readonly IUrlLauncherService _urlLauncherService;
    private bool _isLoading;

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

    public SettingsViewModel(
        ISettingsRepository settingsRepository,
        IBackupService backupService,
        IFilePickerService filePickerService,
        IDialogService dialogService,
        IStorageLocationProvider storageLocationProvider,
        FileBasedRepository noteRepository,
        IMessenger messenger,
        IUrlLauncherService urlLauncherService)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        ArgumentNullException.ThrowIfNull(backupService);
        ArgumentNullException.ThrowIfNull(filePickerService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(storageLocationProvider);
        ArgumentNullException.ThrowIfNull(noteRepository);
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(urlLauncherService);

        _settingsRepository = settingsRepository;
        _backupService = backupService;
        _filePickerService = filePickerService;
        _dialogService = dialogService;
        _storageLocationProvider = storageLocationProvider;
        _noteRepository = noteRepository;
        _messenger = messenger;
        _urlLauncherService = urlLauncherService;
    }

    /// <summary>
    /// Loads persisted settings from disk and populates the bound properties, without
    /// triggering a redundant auto-save back to disk.
    /// </summary>
    public async Task InitializeAsync()
    {
        _isLoading = true;
        try
        {
            var settings = await _settingsRepository.LoadAsync();
            LaunchAtStartup = settings.LaunchAtStartup;
            SelectedDefaultColor = settings.DefaultNoteColor;
        }
        finally
        {
            _isLoading = false;
        }
    }

    partial void OnLaunchAtStartupChanged(bool value)
    {
        if (!_isLoading)
            _ = SaveAsync();
    }

    partial void OnSelectedDefaultColorChanged(uint value)
    {
        if (!_isLoading)
            _ = SaveAsync();
    }

    /// <summary>
    /// Persists the current settings snapshot to disk. Failures are logged rather than
    /// surfaced, so a transient disk error doesn't crash the UI thread.
    /// </summary>
    private async Task SaveAsync()
    {
        try
        {
            await _settingsRepository.SaveAsync(new AppSettings
            {
                LaunchAtStartup = LaunchAtStartup,
                DefaultNoteColor = SelectedDefaultColor
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SettingsViewModel: Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Selects the default note color. Persisted automatically via <see cref="OnSelectedDefaultColorChanged"/>.
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

    /// <summary>
    /// Opens the Privacy Policy in the user's default browser.
    /// </summary>
    [RelayCommand]
    public void OpenPrivacyPolicy()
    {
        _urlLauncherService.OpenUrl(PrivacyPolicyUrl);
    }

    /// <summary>
    /// Opens the Terms of Service in the user's default browser.
    /// </summary>
    [RelayCommand]
    public void OpenTermsOfService()
    {
        _urlLauncherService.OpenUrl(TermsOfServiceUrl);
    }

    /// <summary>
    /// Exports all notes as a zip archive of the note data files, saved to a location chosen
    /// by the user via a Save File dialog defaulted to <see cref="IStorageLocationProvider.BackupsDirectory"/>.
    /// </summary>
    [RelayCommand]
    public async Task ExportNotesAsync()
    {
        var backupsDirectory = _storageLocationProvider.BackupsDirectory;
        Directory.CreateDirectory(backupsDirectory);

        var defaultFileName = $"StickyDo_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        var filePath = _filePickerService.ShowSaveFileDialog(
            defaultFileName,
            Resources.Resources.Export_FileFilter,
            backupsDirectory);

        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            var exportedCount = await _backupService.ExportAsync(filePath, GetRawApplicationVersion());
            await _dialogService.ShowMessageAsync(
                Resources.Resources.Export_SuccessTitle,
                string.Format(Resources.Resources.Export_SuccessMessage, exportedCount, Path.GetFileName(filePath)),
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(ExportNotesAsync));
            await _dialogService.ShowMessageAsync(
                Resources.Resources.Export_ErrorTitle,
                string.Format(Resources.Resources.Export_ErrorMessage, ex.Message),
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Restores notes from a backup archive chosen by the user via an Open File dialog defaulted
    /// to <see cref="IStorageLocationProvider.BackupsDirectory"/>. Reloads the note repository
    /// from disk and broadcasts <see cref="NotesImportedMessage"/> afterward so the notes list
    /// reflects the imported notes immediately, without an app restart.
    /// </summary>
    [RelayCommand]
    public async Task ImportNotesAsync()
    {
        var backupsDirectory = _storageLocationProvider.BackupsDirectory;
        var filePath = _filePickerService.ShowOpenFileDialog(
            Resources.Resources.Import_FileFilter,
            Directory.Exists(backupsDirectory) ? backupsDirectory : null);

        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            var importedCount = await _backupService.ImportAsync(filePath);
            await _noteRepository.ReloadFromDiskAsync();
            _messenger.Send(new NotesImportedMessage(importedCount));

            await _dialogService.ShowMessageAsync(
                Resources.Resources.Import_SuccessTitle,
                string.Format(Resources.Resources.Import_SuccessMessage, importedCount),
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(ImportNotesAsync));
            await _dialogService.ShowMessageAsync(
                Resources.Resources.Import_ErrorTitle,
                string.Format(Resources.Resources.Import_ErrorMessage, ex.Message),
                MessageBoxImage.Error);
        }
    }

    private static string GetApplicationVersion() => $"Version {GetRawApplicationVersion()}";

    private static string GetRawApplicationVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? string.Empty : version.ToString(3);
    }
}
