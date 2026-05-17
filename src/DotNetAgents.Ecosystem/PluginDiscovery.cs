using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Ecosystem;

/// <summary>
/// Discovers plugins from loaded assemblies.
/// </summary>
public class PluginDiscovery : IPluginDiscovery
{
    private readonly ILogger<PluginDiscovery>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginDiscovery"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public PluginDiscovery(ILogger<PluginDiscovery>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IEnumerable<Type> DiscoverPluginTypes(IEnumerable<Assembly>? assemblies = null)
    {
        var assembliesToScan = assemblies ?? AppDomain.CurrentDomain.GetAssemblies();

        var pluginTypes = new List<Type>();

        foreach (var assembly in assembliesToScan)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(type => typeof(IPlugin).IsAssignableFrom(type) &&
                                  !type.IsInterface &&
                                  !type.IsAbstract);

                pluginTypes.AddRange(types);

                _logger?.LogDebug(
                    "Discovered {Count} plugin types in assembly {AssemblyName}",
                    types.Count(),
                    assembly.GetName().Name);
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Failed to load types from assembly {AssemblyName}. Some plugins may not be discovered.",
                    assembly.GetName().Name);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Error scanning assembly {AssemblyName} for plugins",
                    assembly.GetName().Name);
            }
        }

        _logger?.LogInformation(
            "Discovered {Count} plugin types across {AssemblyCount} assemblies",
            pluginTypes.Count,
            assembliesToScan.Count());

        return pluginTypes;
    }

    /// <inheritdoc />
    public IEnumerable<IPlugin> CreatePluginInstances(IEnumerable<Type> pluginTypes, IServiceProvider serviceProvider)
    {
        var plugins = new List<IPlugin>();

        foreach (var pluginType in pluginTypes)
        {
            try
            {
                // Try to create instance using DI
                var plugin = ActivatorUtilities.CreateInstance(serviceProvider, pluginType) as IPlugin;

                if (plugin != null)
                {
                    plugins.Add(plugin);
                    _logger?.LogDebug("Created plugin instance: {PluginId}", plugin.Id);
                }
                else
                {
                    _logger?.LogWarning("Failed to create plugin instance from type {TypeName}", pluginType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Failed to create plugin instance from type {TypeName}",
                    pluginType.Name);
            }
        }

        return plugins;
    }
}

/// <summary>
/// Interface for plugin discovery.
/// </summary>
public interface IPluginDiscovery
{
    /// <summary>
    /// Discovers plugin types from assemblies.
    /// </summary>
    /// <param name="assemblies">Optional list of assemblies to scan. If null, scans all loaded assemblies.</param>
    /// <returns>Collection of plugin types.</returns>
    IEnumerable<Type> DiscoverPluginTypes(IEnumerable<Assembly>? assemblies = null);

    /// <summary>
    /// Creates plugin instances from plugin types.
    /// </summary>
    /// <param name="pluginTypes">The plugin types to instantiate.</param>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <returns>Collection of plugin instances.</returns>
    IEnumerable<IPlugin> CreatePluginInstances(IEnumerable<Type> pluginTypes, IServiceProvider serviceProvider);
}
