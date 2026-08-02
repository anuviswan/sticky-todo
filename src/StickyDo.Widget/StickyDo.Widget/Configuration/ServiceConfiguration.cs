using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using StickyDo.Domain.Repositories;
using StickyDo.Domain.Services;
using StickyDo.Domain.Storage;
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
        ConfigureSettings(services);
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
        var storageLocationProvider = new StorageLocationProvider();
        services.AddSingleton<IStorageLocationProvider>(storageLocationProvider);

        System.Diagnostics.Debug.WriteLine("Initializing file-based repository...");
        var fileBasedRepository = new FileBasedRepository(storageLocationProvider);

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
    /// Registers persistence and auto-save services. The concrete <see cref="PersistenceService"/>
    /// stays Domain-pure (no dependency on CommunityToolkit.Mvvm); <see cref="IPersistenceService"/>
    /// resolves to a <see cref="MessengerBridgedPersistenceService"/> decorator that forwards its
    /// <see cref="PersistenceService.NoteSaveStateChanged"/> event onto the UI-facing messenger, so
    /// note windows can subscribe without the Domain project taking on that dependency.
    /// </summary>
    private static void ConfigurePersistence(IServiceCollection services)
    {
        services.AddSingleton<PersistenceService>();

        services.AddSingleton<IPersistenceService>(sp =>
            new MessengerBridgedPersistenceService(
                sp.GetRequiredService<PersistenceService>(),
                sp.GetRequiredService<IMessenger>()));
    }

    /// <summary>
    /// Registers the settings repository and a fully-loaded <see cref="SettingsViewModel"/>.
    /// The view model's initial load runs synchronously on a thread-pool thread (same
    /// deadlock-avoidance reasoning as <see cref="ConfigureRepositories"/>'s repository
    /// initialization) so consumers always see persisted settings, not defaults that later
    /// change out from under them.
    /// </summary>
    private static void ConfigureSettings(IServiceCollection services)
    {
        services.AddSingleton<ISettingsRepository>(sp =>
            new FileBasedSettingsRepository(sp.GetRequiredService<IStorageLocationProvider>()));

        services.AddSingleton(sp =>
        {
            var viewModel = new SettingsViewModel(sp.GetRequiredService<ISettingsRepository>());
            Task.Run(() => viewModel.InitializeAsync()).Wait(TimeSpan.FromSeconds(10));
            return viewModel;
        });
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
                sp.GetRequiredService<IPersistenceService>(),
                sp.GetRequiredService<IMessenger>(),
                sp.GetRequiredService<IWindowService>()));

        services.AddSingleton<IStickyNoteCreationService, StickyNoteCreationService>();
        services.AddSingleton<MainWindow>();
    }
}
