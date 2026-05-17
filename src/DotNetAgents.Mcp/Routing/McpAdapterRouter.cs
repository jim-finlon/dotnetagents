using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp.Routing;

/// <summary>
/// Routes intents to the appropriate MCP service adapter.
/// </summary>
public class McpAdapterRouter : IMcpAdapterRouter
{
    private readonly IMcpClientFactory _clientFactory;
    private readonly ILogger<McpAdapterRouter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpAdapterRouter"/> class.
    /// </summary>
    /// <param name="clientFactory">The MCP client factory.</param>
    /// <param name="logger">The logger instance.</param>
    public McpAdapterRouter(
        IMcpClientFactory clientFactory,
        ILogger<McpAdapterRouter> logger)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<object?> ExecuteIntentAsync(
        Intent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        if (string.IsNullOrWhiteSpace(intent.TargetService))
        {
            _logger.LogWarning(
                "Intent {Intent} has no target service specified",
                intent.FullName);
            throw new InvalidOperationException($"Intent {intent.FullName} has no target service");
        }

        _logger.LogInformation(
            "Routing intent: Domain={Domain}, Action={Action}, Service={Service}",
            intent.Domain,
            intent.Action,
            intent.TargetService);

        try
        {
            var client = _clientFactory.GetClient(intent.TargetService);

            // Determine tool name (use intent.Tool if specified, otherwise use FullName)
            var toolName = intent.Tool ?? intent.FullName;

            // Create tool call request
            var request = new McpToolCallRequest
            {
                Tool = toolName,
                Arguments = intent.Parameters,
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Call the tool
            var response = await client.CallToolAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                _logger.LogError(
                    "Tool call failed: {Tool} on {Service} - {Error}",
                    toolName,
                    intent.TargetService,
                    response.Error);
                throw new InvalidOperationException(
                    $"Tool call failed: {response.Error}");
            }

            return response.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to execute intent: {Domain}.{Action}",
                intent.Domain,
                intent.Action);
            throw;
        }
    }
}
