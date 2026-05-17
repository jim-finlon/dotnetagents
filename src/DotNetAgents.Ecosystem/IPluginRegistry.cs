namespace DotNetAgents.Ecosystem;

/// <summary>
/// Registry for managing plugins.
/// </summary>
public interface IPluginRegistry
{
    /// <summary>
    /// Registers a plugin.
    /// </summary>
    /// <param name="plugin">The plugin to register.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RegisterAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID to unregister.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnregisterAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a plugin by ID.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The plugin, or null if not found.</returns>
    Task<IPlugin?> GetAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered plugins.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>List of all plugins.</returns>
    Task<IReadOnlyList<IPlugin>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets plugins by category.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>List of plugins in the category.</returns>
    Task<IReadOnlyList<IPlugin>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
}

/// <summary>
/// Plugin metadata.
/// </summary>
public class PluginMetadata
{
    /// <summary>
    /// Gets or sets the plugin ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin author.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin license.
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the plugin dependencies (plugin IDs that must be loaded first).
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// Gets or sets the plugin repository URL.
    /// </summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// Gets or sets the plugin documentation URL.
    /// </summary>
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// Gets or sets the minimum required DotNetAgents.Core version.
    /// </summary>
    public string? MinimumCoreVersion { get; set; }
}
