using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DotNetAgents.Ecosystem;

/// <summary>
/// Hosted service that discovers and registers plugins on startup.
/// </summary>
internal class PluginDiscoveryService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPluginDiscovery _pluginDiscovery;
    private readonly IPluginRegistry _pluginRegistry;
    private readonly ILogger<PluginDiscoveryService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginDiscoveryService"/> class.
    /// </summary>
    public PluginDiscoveryService(
        IServiceProvider serviceProvider,
        IPluginDiscovery pluginDiscovery,
        IPluginRegistry pluginRegistry,
        ILogger<PluginDiscoveryService>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _pluginDiscovery = pluginDiscovery ?? throw new ArgumentNullException(nameof(pluginDiscovery));
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting plugin discovery...");

        // Discover plugin types from all loaded assemblies
        var pluginTypes = _pluginDiscovery.DiscoverPluginTypes();

        // Create plugin instances
        var pluginInstances = _pluginDiscovery.CreatePluginInstances(pluginTypes, _serviceProvider);

        // Register plugins
        foreach (var plugin in pluginInstances)
        {
            await _pluginRegistry.RegisterAsync(plugin, cancellationToken).ConfigureAwait(false);
        }

        _logger?.LogInformation("Plugin discovery completed. Registered {Count} plugins.", pluginInstances.Count());
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
