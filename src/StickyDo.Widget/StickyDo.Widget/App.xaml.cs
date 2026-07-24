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
        _serviceProvider?.Dispose();
        ReleaseSingleInstanceLock();
        base.OnExit(e);
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
        mainWindow.Show();

        // Load notes - DataContext is set in MainWindow constructor
        if (mainWindow.DataContext is MainWindowViewModel viewModel)
        {
            _ = viewModel.LoadNotesAsync();
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
