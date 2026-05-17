using Microsoft.Extensions.Logging;

namespace DotNetAgents.Ecosystem;

/// <summary>
/// Resolves plugin dependencies and determines initialization order.
/// </summary>
public class PluginDependencyResolver : IPluginDependencyResolver
{
    private readonly ILogger<PluginDependencyResolver>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginDependencyResolver"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public PluginDependencyResolver(ILogger<PluginDependencyResolver>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<IPlugin> ResolveDependencies(IEnumerable<IPlugin> plugins)
    {
        var pluginList = plugins.ToList();
        var pluginMap = pluginList.ToDictionary(p => p.Id, p => p);
        var resolved = new List<IPlugin>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        // Get dependencies for each plugin
        var dependencies = new Dictionary<string, List<string>>();
        foreach (var plugin in pluginList)
        {
            if (plugin is IPluginWithMetadata pluginWithMetadata)
            {
                dependencies[plugin.Id] = pluginWithMetadata.Metadata.Dependencies ?? new List<string>();
            }
            else
            {
                dependencies[plugin.Id] = new List<string>();
            }
        }

        // Topological sort
        foreach (var plugin in pluginList)
        {
            if (!visited.Contains(plugin.Id))
            {
                Visit(plugin, pluginMap, dependencies, visited, visiting, resolved);
            }
        }

        _logger?.LogInformation(
            "Resolved plugin dependencies. Initialization order: {Order}",
            string.Join(" -> ", resolved.Select(p => p.Id)));

        return resolved;
    }

    /// <inheritdoc />
    public bool ValidateDependencies(IEnumerable<IPlugin> plugins, out IReadOnlyList<string> missingDependencies)
    {
        var pluginList = plugins.ToList();
        var pluginIds = new HashSet<string>(pluginList.Select(p => p.Id));
        var missing = new List<string>();

        foreach (var plugin in pluginList)
        {
            if (plugin is IPluginWithMetadata pluginWithMetadata)
            {
                var deps = pluginWithMetadata.Metadata.Dependencies ?? new List<string>();
                foreach (var dep in deps)
                {
                    if (!pluginIds.Contains(dep))
                    {
                        missing.Add($"{plugin.Id} requires {dep}");
                    }
                }
            }
        }

        missingDependencies = missing;

        if (missing.Count > 0)
        {
            _logger?.LogWarning(
                "Plugin dependency validation failed. Missing dependencies: {Dependencies}",
                string.Join(", ", missing));
            return false;
        }

        return true;
    }

    private void Visit(
        IPlugin plugin,
        Dictionary<string, IPlugin> pluginMap,
        Dictionary<string, List<string>> dependencies,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<IPlugin> resolved)
    {
        if (visited.Contains(plugin.Id))
            return;

        if (visiting.Contains(plugin.Id))
        {
            _logger?.LogError(
                "Circular dependency detected involving plugin {PluginId}",
                plugin.Id);
            throw new InvalidOperationException(
                $"Circular dependency detected involving plugin {plugin.Id}");
        }

        visiting.Add(plugin.Id);

        // Visit dependencies first
        if (dependencies.TryGetValue(plugin.Id, out var deps))
        {
            foreach (var depId in deps)
            {
                if (pluginMap.TryGetValue(depId, out var depPlugin))
                {
                    Visit(depPlugin, pluginMap, dependencies, visited, visiting, resolved);
                }
                else
                {
                    _logger?.LogWarning(
                        "Plugin {PluginId} depends on {DependencyId}, but dependency not found",
                        plugin.Id,
                        depId);
                }
            }
        }

        visiting.Remove(plugin.Id);
        visited.Add(plugin.Id);
        resolved.Add(plugin);
    }
}

/// <summary>
/// Interface for resolving plugin dependencies.
/// </summary>
public interface IPluginDependencyResolver
{
    /// <summary>
    /// Resolves plugin dependencies and returns plugins in initialization order.
    /// </summary>
    /// <param name="plugins">The plugins to resolve.</param>
    /// <returns>Plugins in dependency order (dependencies first).</returns>
    IReadOnlyList<IPlugin> ResolveDependencies(IEnumerable<IPlugin> plugins);

    /// <summary>
    /// Validates that all plugin dependencies are satisfied.
    /// </summary>
    /// <param name="plugins">The plugins to validate.</param>
    /// <param name="missingDependencies">Output list of missing dependencies.</param>
    /// <returns>True if all dependencies are satisfied, false otherwise.</returns>
    bool ValidateDependencies(IEnumerable<IPlugin> plugins, out IReadOnlyList<string> missingDependencies);
}
