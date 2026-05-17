using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp;

/// <summary>
/// Registry that aggregates tools from all MCP services.
/// </summary>
public class McpToolRegistry : IMcpToolRegistry
{
    private readonly IMcpClientFactory _clientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<McpToolRegistry> _logger;
    private const string CacheKey = "mcp_tools_cache";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolRegistry"/> class.
    /// </summary>
    /// <param name="clientFactory">The MCP client factory.</param>
    /// <param name="cache">The memory cache.</param>
    /// <param name="logger">The logger instance.</param>
    public McpToolRegistry(
        IMcpClientFactory clientFactory,
        IMemoryCache cache,
        ILogger<McpToolRegistry> logger)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<List<McpToolDefinition>> GetAllToolsAsync(
        CancellationToken cancellationToken = default)
    {
        // Try to get from cache
        if (_cache.TryGetValue<List<McpToolDefinition>>(CacheKey, out var cachedTools) &&
            cachedTools != null)
        {
            _logger.LogDebug("Returning {Count} tools from cache", cachedTools.Count);
            return cachedTools;
        }

        // Refresh cache
        await RefreshToolsAsync(cancellationToken).ConfigureAwait(false);

        // Return from cache (should now be populated)
        return _cache.Get<List<McpToolDefinition>>(CacheKey) ?? new List<McpToolDefinition>();
    }

    /// <inheritdoc />
    public async Task<List<McpToolDefinition>> GetToolsForServiceAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var allTools = await GetAllToolsAsync(cancellationToken).ConfigureAwait(false);
        return allTools.Where(t => t.ServiceName == serviceName).ToList();
    }

    /// <inheritdoc />
    public async Task<McpToolDefinition?> FindToolAsync(
        string toolName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var allTools = await GetAllToolsAsync(cancellationToken).ConfigureAwait(false);
        return allTools.FirstOrDefault(t =>
            t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task RefreshToolsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing tool registry from all MCP services");

        var allTools = new List<McpToolDefinition>();
        var services = _clientFactory.GetRegisteredServices();

        foreach (var serviceName in services)
        {
            try
            {
                var client = _clientFactory.GetClient(serviceName);
                var response = await client.ListToolsAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "Loaded {Count} tools from {ServiceName}",
                    response.Tools.Count,
                    serviceName);

                allTools.AddRange(response.Tools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load tools from {ServiceName}", serviceName);
                // Continue with other services
            }
        }

        // Update cache
        _cache.Set(CacheKey, allTools, CacheDuration);

        _logger.LogInformation(
            "Tool registry refreshed: {TotalTools} tools from {ServiceCount} services",
            allTools.Count,
            services.Count());
    }
}
