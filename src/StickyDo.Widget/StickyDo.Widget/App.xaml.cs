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
    private PersistenceService? _persistenceService;
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
            StartAutoSave();
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
        StopAutoSaveAsync();
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

        // Initialize file-based repository
        System.Diagnostics.Debug.WriteLine("Initializing file-based repository...");
        var fileBasedRepository = new FileBasedRepository();
        fileBasedRepository.InitializeAsync().Wait(TimeSpan.FromSeconds(10));
        System.Diagnostics.Debug.WriteLine("Repository initialized successfully.");

        // Register repositories using interface-based pattern
        services.AddSingleton<IStickyNoteRepository>(fileBasedRepository);
        services.AddSingleton<IStickyNoteTaskRepository>(fileBasedRepository);
        services.AddSingleton(fileBasedRepository);

        // Register persistence service
        services.AddSingleton(new PersistenceService(fileBasedRepository));

        // Register dialog and window services first (used by other services)
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IWindowService, WindowService>();

        // Register core services
        services.AddSingleton<StickyNoteService>();
        services.AddSingleton<WindowManager>();

        // Register with factory to support Lazy<T> and break circular dependency
        services.AddSingleton<IStickyNoteWindowService>(sp =>
            new StickyNoteWindowService(
                sp.GetRequiredService<StickyNoteService>(),
                sp.GetRequiredService<WindowManager>(),
                sp.GetRequiredService<IDialogService>(),
                sp.GetRequiredService<IWindowService>(),
                new Lazy<IStickyNoteCreationService>(() => sp.GetRequiredService<IStickyNoteCreationService>())));

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

    private void StartAutoSave()
    {
        if (_serviceProvider == null)
            return;

        try
        {
            _persistenceService = _serviceProvider.GetService<PersistenceService>();
            if (_persistenceService != null)
            {
                System.Diagnostics.Debug.WriteLine("Starting auto-save service...");
                _persistenceService.StartAutoSave();
                System.Diagnostics.Debug.WriteLine("Auto-save started successfully.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Auto-save disabled - PersistenceService not available");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting auto-save: {ex}");
        }
    }

    private void StopAutoSaveAsync()
    {
        if (_persistenceService == null)
            return;

        try
        {
            _persistenceService.StopAutoSaveAsync().Wait(TimeSpan.FromSeconds(5));
            _persistenceService.SaveAllDirtyNotesAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during shutdown persistence: {ex}");
        }
    }
}
