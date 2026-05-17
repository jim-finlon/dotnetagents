using Microsoft.Extensions.Logging;

namespace DotNetAgents.Ecosystem;

/// <summary>
/// In-memory implementation of plugin registry.
/// </summary>
public class PluginRegistry : IPluginRegistry
{
    private readonly Dictionary<string, IPlugin> _plugins = new();
    private readonly Dictionary<string, List<string>> _categoryIndex = new();
    private readonly ILogger<PluginRegistry>? _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginRegistry"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public PluginRegistry(ILogger<PluginRegistry>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task RegisterAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_plugins.ContainsKey(plugin.Id))
            {
                throw new InvalidOperationException($"Plugin '{plugin.Id}' is already registered.");
            }

            _plugins[plugin.Id] = plugin;

            // Index by category if plugin has category metadata
            if (plugin is IPluginWithMetadata pluginWithMetadata)
            {
                var category = pluginWithMetadata.Metadata.Category;
                if (!string.IsNullOrEmpty(category))
                {
                    if (!_categoryIndex.TryGetValue(category, out var plugins))
                    {
                        plugins = new List<string>();
                        _categoryIndex[category] = plugins;
                    }
                    plugins.Add(plugin.Id);
                }
            }

            _logger?.LogInformation("Registered plugin {PluginId} ({PluginName})", plugin.Id, plugin.Name);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(pluginId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_plugins.TryGetValue(pluginId, out var plugin))
            {
                _plugins.Remove(pluginId);

                // Remove from category index
                if (plugin is IPluginWithMetadata pluginWithMetadata)
                {
                    var category = pluginWithMetadata.Metadata.Category;
                    if (!string.IsNullOrEmpty(category) && _categoryIndex.TryGetValue(category, out var plugins))
                    {
                        plugins.Remove(pluginId);
                    }
                }

                _logger?.LogInformation("Unregistered plugin {PluginId}", pluginId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IPlugin?> GetAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(pluginId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _plugins.TryGetValue(pluginId, out var plugin);
            return Task.FromResult<IPlugin?>(plugin);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IPlugin>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<IPlugin>>(_plugins.Values.ToList());
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IPlugin>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(category);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_categoryIndex.TryGetValue(category, out var pluginIds))
            {
                var plugins = pluginIds
                    .Select(id => _plugins.TryGetValue(id, out var plugin) ? plugin : null)
                    .Where(p => p != null)
                    .Cast<IPlugin>()
                    .ToList();

                return Task.FromResult<IReadOnlyList<IPlugin>>(plugins);
            }
        }

        return Task.FromResult<IReadOnlyList<IPlugin>>(new List<IPlugin>());
    }
}

/// <summary>
/// Plugin with metadata support.
/// </summary>
public interface IPluginWithMetadata : IPlugin
{
    /// <summary>
    /// Gets the plugin metadata.
    /// </summary>
    PluginMetadata Metadata { get; }
}
