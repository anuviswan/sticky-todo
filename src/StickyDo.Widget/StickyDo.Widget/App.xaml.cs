using System.Windows;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
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
                var dialogService = new DialogService();
                _ = dialogService.ShowMessageAsync("Application Running", "Sticky TODO is already running.", MessageBoxImage.Information);
                Shutdown(1);
                return;
            }

            ConfigureServices();
            InitializeMainWindow();
        }
        catch (Exception ex)
        {
            var dialogService = new DialogService();
            _ = dialogService.ShowMessageAsync("Startup Error", $"Failed to start application: {ex.Message}", MessageBoxImage.Error);
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

        // Register repositories - InMemoryRepository implements both interfaces
        var inMemoryRepository = new InMemoryRepository();
        services.AddSingleton<IStickyNoteRepository>(inMemoryRepository);
        services.AddSingleton<IStickyNoteTaskRepository>(inMemoryRepository);

        // Register dialog and window services first (used by other services)
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IWindowService, WindowService>();

        // Register core services
        services.AddSingleton<StickyNoteService>();
        services.AddSingleton<WindowManager>();
        services.AddSingleton<IStickyNoteWindowService, StickyNoteWindowService>();
        services.AddSingleton<IStickyNoteCreationService, StickyNoteCreationService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    private void InitializeMainWindow()
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Services not configured");

        var mainWindow = new MainWindow();
        var windowManager = _serviceProvider.GetRequiredService<WindowManager>();
        windowManager.SetMainWindow(mainWindow);

        var windowServiceImpl = _serviceProvider.GetRequiredService<IWindowService>();
        if (windowServiceImpl is WindowService windowService)
        {
            windowService.SetMainWindow(mainWindow);
        }

        var stickyNoteService = _serviceProvider.GetRequiredService<StickyNoteService>();
        var noteWindowService = _serviceProvider.GetRequiredService<IStickyNoteWindowService>();
        var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        var mainWindowService = _serviceProvider.GetRequiredService<IWindowService>();

        var notesListViewModel = new NotesListViewModel(stickyNoteService, noteWindowService, dialogService);
        var viewModel = new MainWindowViewModel(mainWindowService, notesListViewModel);

        mainWindow.SetViewModel(viewModel);
        MainWindow = mainWindow;
        mainWindow.Show();

        _ = viewModel.LoadNotesAsync();
    }
}
