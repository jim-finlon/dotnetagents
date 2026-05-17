using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Configuration;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp;

/// <summary>
/// Factory for creating and managing MCP clients.
/// </summary>
public class McpClientFactory : IMcpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<McpClient> _clientLogger;
    private readonly Dictionary<string, McpServiceConfig> _serviceConfigs;
    private readonly Dictionary<string, IMcpClient> _clients = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="McpClientFactory"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="serviceConfigs">The service configurations.</param>
    /// <param name="clientLogger">The logger for MCP clients.</param>
    public McpClientFactory(
        IHttpClientFactory httpClientFactory,
        IEnumerable<McpServiceConfig> serviceConfigs,
        ILogger<McpClient> clientLogger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _clientLogger = clientLogger ?? throw new ArgumentNullException(nameof(clientLogger));
        _serviceConfigs = serviceConfigs.ToDictionary(c => c.ServiceName, c => c);
    }

    /// <inheritdoc />
    public IMcpClient GetClient(string serviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        // Check if client already exists
        if (_clients.TryGetValue(serviceName, out var existingClient))
        {
            return existingClient;
        }

        lock (_lock)
        {
            // Double-check after acquiring lock
            if (_clients.TryGetValue(serviceName, out existingClient))
            {
                return existingClient;
            }

            // Get service config
            if (!_serviceConfigs.TryGetValue(serviceName, out var config))
            {
                throw new InvalidOperationException(
                    $"Service '{serviceName}' is not registered. " +
                    $"Available services: {string.Join(", ", _serviceConfigs.Keys)}");
            }

            // Create new client
            var httpClient = _httpClientFactory.CreateClient(serviceName);
            var client = new McpClient(httpClient, config, _clientLogger);

            _clients[serviceName] = client;
            return client;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRegisteredServices()
    {
        return _serviceConfigs.Keys;
    }
}
