using Microsoft.Extensions.Logging;

namespace DotNetAgents.Ecosystem;

/// <summary>
/// Base class for plugins that provides common functionality.
/// </summary>
public abstract class PluginBase : IPluginWithMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginBase"/> class.
    /// </summary>
    protected PluginBase()
    {
        Metadata = new PluginMetadata
        {
            Id = GetType().Name.Replace("Plugin", "").ToLowerInvariant(),
            Name = GetType().Name.Replace("Plugin", ""),
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Description = string.Empty,
            Author = "DotNetAgents",
            License = "MIT",
            Category = "General",
            Tags = new List<string>(),
            Dependencies = new List<string>()
        };
    }

    /// <inheritdoc />
    public string Id => Metadata.Id;

    /// <inheritdoc />
    public string Name => Metadata.Name;

    /// <inheritdoc />
    public string Version => Metadata.Version;

    /// <inheritdoc />
    public string Description => Metadata.Description;

    /// <inheritdoc />
    public string Author => Metadata.Author;

    /// <inheritdoc />
    public string License => Metadata.License;

    /// <inheritdoc />
    public PluginMetadata Metadata { get; protected set; }

    /// <summary>
    /// Gets the logger for this plugin.
    /// </summary>
    protected ILogger? Logger { get; private set; }

    /// <inheritdoc />
    public virtual Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        Logger = context.LoggerFactory.CreateLogger(GetType());

        Logger?.LogInformation(
            "Initializing plugin {PluginId} ({PluginName}) version {Version}",
            Id,
            Name,
            Version);

        return OnInitializeAsync(context, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Logger?.LogInformation(
            "Shutting down plugin {PluginId} ({PluginName})",
            Id,
            Name);

        return OnShutdownAsync(cancellationToken);
    }

    /// <summary>
    /// Called when the plugin is being initialized.
    /// </summary>
    /// <param name="context">The plugin context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnInitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the plugin is being shut down.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
