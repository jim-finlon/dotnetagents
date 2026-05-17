using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Ecosystem;

/// <summary>
/// Base interface for DotNetAgents plugins.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Gets the plugin identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the plugin name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the plugin version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the plugin description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the plugin author.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets the plugin license.
    /// </summary>
    string License { get; }

    /// <summary>
    /// Initializes the plugin.
    /// </summary>
    /// <param name="context">The plugin context.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the plugin.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Context provided to plugins during initialization.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets the configuration for the plugin.
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the logger factory.
    /// </summary>
    ILoggerFactory LoggerFactory { get; }
}
