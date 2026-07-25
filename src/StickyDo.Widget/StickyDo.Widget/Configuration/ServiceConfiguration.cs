using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Services;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Configuration;

/// <summary>
/// Configures all application services organized by category.
/// Handles dependency injection setup for repositories, services, and view models.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configures all services and returns a configured ServiceProvider.
    /// </summary>
    public static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        ConfigureRepositories(services);
        ConfigurePersistence(services);
        ConfigureDialogAndWindow(services);
        ConfigureCore(services);
        ConfigureViewModels(services);
        ConfigureUI(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Registers repository implementations.
    /// </summary>
    private static void ConfigureRepositories(IServiceCollection services)
    {
        System.Diagnostics.Debug.WriteLine("Initializing file-based repository...");
        var fileBasedRepository = new FileBasedRepository();

        // Run on a thread-pool thread (no captured WPF DispatcherSynchronizationContext) to avoid
        // a sync-over-async deadlock: blocking the UI thread here while InitializeAsync's internal
        // awaits try to resume back on that same (blocked) UI thread would hang until timeout.
        Task.Run(() => fileBasedRepository.InitializeAsync()).Wait(TimeSpan.FromSeconds(10));
        System.Diagnostics.Debug.WriteLine("Repository initialized successfully.");

        services.AddSingleton<IStickyNoteRepository>(fileBasedRepository);
        services.AddSingleton<IStickyNoteTaskRepository>(fileBasedRepository);
        services.AddSingleton(fileBasedRepository);
    }

    /// <summary>
    /// Registers persistence and auto-save services.
    /// </summary>
    private static void ConfigurePersistence(IServiceCollection services)
    {
        services.AddSingleton<PersistenceService>();
    }

    /// <summary>
    /// Registers dialog and window services (used by other services).
    /// </summary>
    private static void ConfigureDialogAndWindow(IServiceCollection services)
    {
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();
    }

    /// <summary>
    /// Registers core domain and business logic services.
    /// </summary>
    private static void ConfigureCore(IServiceCollection services)
    {
        services.AddSingleton<StickyNoteService>();
        services.AddSingleton<StickyNoteTaskService>();
        services.AddSingleton<WindowManager>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
    }

    /// <summary>
    /// Registers view models.
    /// </summary>
    private static void ConfigureViewModels(IServiceCollection services)
    {
        services.AddSingleton<NotesListViewModel>();
        services.AddSingleton<MainWindowViewModel>();
    }

    /// <summary>
    /// Registers window services with factory pattern for dependency resolution.
    /// </summary>
    private static void ConfigureUI(IServiceCollection services)
    {
        services.AddSingleton<IStickyNoteWindowService>(sp =>
            new StickyNoteWindowService(
                sp.GetRequiredService<StickyNoteService>(),
                sp.GetRequiredService<StickyNoteTaskService>(),
                sp.GetRequiredService<WindowManager>(),
                sp.GetRequiredService<IDialogService>(),
                new Lazy<IStickyNoteCreationService>(() => sp.GetRequiredService<IStickyNoteCreationService>()),
                sp.GetRequiredService<PersistenceService>(),
                sp.GetRequiredService<IMessenger>()));

        services.AddSingleton<IStickyNoteCreationService, StickyNoteCreationService>();
        services.AddSingleton<MainWindow>();
    }
}
