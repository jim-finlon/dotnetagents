using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DotNetAgents.Ecosystem;

/// <summary>
/// Extension methods for registering ecosystem services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ecosystem support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDotNetAgentsEcosystem(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register plugin registry
        services.TryAddSingleton<IPluginRegistry, PluginRegistry>();

        // Register integration marketplace
        services.TryAddSingleton<IIntegrationMarketplace, InMemoryIntegrationMarketplace>();

        // Register plugin discovery
        services.TryAddSingleton<IPluginDiscovery, PluginDiscovery>();

        // Register dependency resolver
        services.TryAddSingleton<IPluginDependencyResolver, PluginDependencyResolver>();

        // Register lifecycle manager
        services.TryAddSingleton<IPluginLifecycleManager, PluginLifecycleManager>();

        return services;
    }

    /// <summary>
    /// Enables automatic plugin discovery from loaded assemblies.
    /// Plugins will be discovered and registered when the service provider is built.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection EnablePluginDiscovery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the hosted service that will discover and register plugins on startup
        services.AddSingleton<PluginDiscoveryService>();
        services.AddHostedService(sp => sp.GetRequiredService<PluginDiscoveryService>());

        return services;
    }

    /// <summary>
    /// Registers a plugin instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="plugin">The plugin to register.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlugin(
        this IServiceCollection services,
        IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(plugin);

        // Register plugin type in DI if not already registered
        var pluginType = plugin.GetType();
        services.TryAddTransient(pluginType, _ => plugin);

        // Register plugin instance in registry (deferred until service provider is built)
        services.AddSingleton(serviceProvider =>
        {
            var registry = serviceProvider.GetRequiredService<IPluginRegistry>();
            registry.RegisterAsync(plugin).GetAwaiter().GetResult();
            return plugin;
        });

        return services;
    }

    /// <summary>
    /// Initializes all registered plugins.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task InitializePluginsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var lifecycleManager = serviceProvider.GetRequiredService<IPluginLifecycleManager>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        await lifecycleManager.InitializePluginsAsync(
            serviceProvider,
            configuration,
            loggerFactory,
            cancellationToken);
    }

    /// <summary>
    /// Shuts down all initialized plugins.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ShutdownPluginsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var lifecycleManager = serviceProvider.GetRequiredService<IPluginLifecycleManager>();
        await lifecycleManager.ShutdownPluginsAsync(cancellationToken);
    }
}
