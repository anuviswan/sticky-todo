using System.Windows;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using StickyDo.Domain.Services;
using StickyDo.Widget.Configuration;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Services;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private ITrayIconService? _trayIconService;
    private bool _isExitRequested;
    private static Mutex? _appMutex;
    private const string MutexName = "StickyDo_SingleInstance_e8d3c9a1";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            if (!AcquireSingleInstanceLock())
            {
                MessageBox.Show("Sticky TODO is already running.", "Application Running", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown(1);
                return;
            }

            ConfigureServices();
            InitializeMainWindow();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Startup Error: {ex}");
            MessageBox.Show($"Failed to start application: {ex.Message}\n\n{ex.StackTrace}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        StopAutoSaveAndSaveAllAsync();
        _trayIconService?.Dispose();
        DisposeServiceProvider();
        ReleaseSingleInstanceLock();
        base.OnExit(e);
    }

    /// <summary>
    /// Disposes the service provider asynchronously. Required because at least one
    /// registered singleton (PersistenceService) only implements IAsyncDisposable;
    /// the synchronous ServiceProvider.Dispose() throws for such services.
    /// </summary>
    private void DisposeServiceProvider()
    {
        if (_serviceProvider == null)
            return;

        try
        {
            ((IAsyncDisposable)_serviceProvider).DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disposing service provider: {ex}");
        }
    }

    private static bool AcquireSingleInstanceLock()
    {
        _appMutex = new Mutex(true, MutexName, out bool createdNew);
        return createdNew;
    }

    private static void ReleaseSingleInstanceLock()
    {
        _appMutex?.ReleaseMutex();
        _appMutex?.Dispose();
        _appMutex = null;
    }

    private void ConfigureServices()
    {
        _serviceProvider = ServiceConfiguration.ConfigureServices();
    }

    private void InitializeMainWindow()
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Services not configured");

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var windowManager = _serviceProvider.GetRequiredService<WindowManager>();
        windowManager.SetMainWindow(mainWindow);

        var windowServiceImpl = _serviceProvider.GetRequiredService<IWindowService>();
        if (windowServiceImpl is WindowService windowService)
        {
            windowService.SetMainWindow(mainWindow);
        }

        MainWindow = mainWindow;

        // Closing the notes list (via its close button or Alt+F4) should only hide it to the
        // tray, not quit the app. Only the tray icon's "Exit" command truly shuts down.
        mainWindow.Closing += MainWindow_Closing;

        _trayIconService = _serviceProvider.GetRequiredService<ITrayIconService>();
        _trayIconService.Initialize(
            onOpenRequested: () => ShowMainWindow(mainWindow),
            onExitRequested: () =>
            {
                _isExitRequested = true;
                Shutdown();
            });

        // Load notes - DataContext is set in MainWindow constructor
        if (mainWindow.DataContext is MainWindowViewModel viewModel)
        {
            _ = viewModel.LoadNotesAsync();
        }

        _ = RestoreOpenNotesOrShowListAsync(mainWindow);
    }

    /// <summary>
    /// Hides the notes list to the tray instead of letting it close, unless the user
    /// chose "Exit" from the tray icon.
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExitRequested)
            return;

        e.Cancel = true;
        ((Window)sender!).Hide();
    }

    /// <summary>
    /// Brings the main (notes list) window to the foreground, restoring it from a
    /// minimized or hidden (tray-only) state if necessary.
    /// </summary>
    private static void ShowMainWindow(Window mainWindow)
    {
        if (mainWindow.WindowState == System.Windows.WindowState.Minimized)
            mainWindow.WindowState = System.Windows.WindowState.Normal;

        if (!mainWindow.IsVisible)
            mainWindow.Show();

        mainWindow.Activate();
    }

    /// <summary>
    /// On startup, reopens any sticky notes left open from the previous session as
    /// floating windows instead of showing the main notes list. If no notes were left
    /// open, the notes list is shown as usual.
    /// </summary>
    private async Task RestoreOpenNotesOrShowListAsync(MainWindow mainWindow)
    {
        if (_serviceProvider == null)
            return;

        try
        {
            var stickyNoteService = _serviceProvider.GetRequiredService<StickyNoteService>();
            var openNoteIds = (await stickyNoteService.GetAllNotesAsync())
                .Where(n => n.IsOpened)
                .Select(n => n.Id)
                .ToList();

            if (openNoteIds.Count == 0)
            {
                mainWindow.Show();
                return;
            }

            var noteWindowService = _serviceProvider.GetRequiredService<IStickyNoteWindowService>();
            foreach (var noteId in openNoteIds)
            {
                await noteWindowService.OpenNoteWindowAsync(noteId);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error restoring open notes: {ex}");
            mainWindow.Show();
        }
    }

    private void StopAutoSaveAndSaveAllAsync()
    {
        if (_serviceProvider == null)
            return;

        try
        {
            var persistenceService = _serviceProvider.GetService<PersistenceService>();
            if (persistenceService != null)
            {
                persistenceService.StopAutoSaveAsync().Wait(TimeSpan.FromSeconds(5));
                persistenceService.SaveAllDirtyNotesAsync().Wait(TimeSpan.FromSeconds(5));
                System.Diagnostics.Debug.WriteLine("All pending changes saved before exit.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during shutdown persistence: {ex}");
        }
    }
}
