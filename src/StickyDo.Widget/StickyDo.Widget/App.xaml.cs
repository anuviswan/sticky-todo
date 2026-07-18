using System.Windows;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Widget.Data;
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
            MessageBox.Show($"Failed to start application: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
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
        var services = new ServiceCollection();

        // Register repositories
        services.AddSingleton<IStickyNoteRepository, InMemoryRepository>();

        // Register services
        services.AddSingleton<StickyNoteService>();

        // Register view models
        services.AddSingleton<MainWindowViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    private void InitializeMainWindow()
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Services not configured");

        var mainWindow = new MainWindow();
        var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

        mainWindow.SetViewModel(viewModel);
        mainWindow.Show();

        _ = viewModel.LoadNotesAsync();
    }
}
