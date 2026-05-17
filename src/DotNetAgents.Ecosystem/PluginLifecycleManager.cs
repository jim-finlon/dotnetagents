using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Ecosystem;

/// <summary>
/// Manages plugin lifecycle (initialization and shutdown).
/// </summary>
public class PluginLifecycleManager : IPluginLifecycleManager
{
    private readonly IPluginRegistry _pluginRegistry;
    private readonly IPluginDependencyResolver _dependencyResolver;
    private readonly ILogger<PluginLifecycleManager>? _logger;
    private readonly List<IPlugin> _initializedPlugins = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLifecycleManager"/> class.
    /// </summary>
    /// <param name="pluginRegistry">The plugin registry.</param>
    /// <param name="dependencyResolver">The dependency resolver.</param>
    /// <param name="logger">Optional logger instance.</param>
    public PluginLifecycleManager(
        IPluginRegistry pluginRegistry,
        IPluginDependencyResolver dependencyResolver,
        ILogger<PluginLifecycleManager>? logger = null)
    {
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
        _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InitializePluginsAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        var allPlugins = await _pluginRegistry.GetAllAsync(cancellationToken);

        // Validate dependencies
        if (!_dependencyResolver.ValidateDependencies(allPlugins, out var missingDeps))
        {
            throw new InvalidOperationException(
                $"Plugin dependencies not satisfied: {string.Join(", ", missingDeps)}");
        }

        // Resolve initialization order
        var orderedPlugins = _dependencyResolver.ResolveDependencies(allPlugins);

        // Initialize in dependency order
        foreach (var plugin in orderedPlugins)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var context = new PluginContext(serviceProvider, configuration, loggerFactory);
                await plugin.InitializeAsync(context, cancellationToken);

                _initializedPlugins.Add(plugin);

                _logger?.LogInformation(
                    "Initialized plugin {PluginId} ({PluginName})",
                    plugin.Id,
                    plugin.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Failed to initialize plugin {PluginId} ({PluginName})",
                    plugin.Id,
                    plugin.Name);
                throw;
            }
        }

        _logger?.LogInformation(
            "Initialized {Count} plugins",
            _initializedPlugins.Count);
    }

    /// <inheritdoc />
    public async Task ShutdownPluginsAsync(CancellationToken cancellationToken = default)
    {
        // Shutdown in reverse order (dependencies last)
        for (int i = _initializedPlugins.Count - 1; i >= 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var plugin = _initializedPlugins[i];

            try
            {
                await plugin.ShutdownAsync(cancellationToken);

                _logger?.LogInformation(
                    "Shut down plugin {PluginId} ({PluginName})",
                    plugin.Id,
                    plugin.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error shutting down plugin {PluginId} ({PluginName})",
                    plugin.Id,
                    plugin.Name);
                // Continue shutting down other plugins
            }
        }

        _initializedPlugins.Clear();

        _logger?.LogInformation("Shut down all plugins");
    }
}

/// <summary>
/// Interface for managing plugin lifecycle.
/// </summary>
public interface IPluginLifecycleManager
{
    /// <summary>
    /// Initializes all registered plugins in dependency order.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializePluginsAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down all initialized plugins in reverse dependency order.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShutdownPluginsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of IPluginContext.
/// </summary>
internal class PluginContext : IPluginContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginContext"/> class.
    /// </summary>
    public PluginContext(
        IServiceProvider services,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public IServiceProvider Services { get; }

    /// <inheritdoc />
    public IConfiguration Configuration { get; }

    /// <inheritdoc />
    public ILoggerFactory LoggerFactory { get; }
}
