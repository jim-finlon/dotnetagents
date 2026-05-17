using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Configuration;
using DotNetAgents.Mcp.Exceptions;
using DotNetAgents.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp;

/// <summary>
/// HTTP-based MCP client implementation.
/// </summary>
public class McpClient : IMcpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpClient> _logger;
    private readonly McpServiceConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <inheritdoc />
    public string ServiceName => _config.ServiceName;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="config">The service configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public McpClient(
        HttpClient httpClient,
        McpServiceConfig config,
        ILogger<McpClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

        // Add auth header if configured
        if (!string.IsNullOrEmpty(config.AuthToken))
        {
            _httpClient.DefaultRequestHeaders.Add(
                "Authorization",
                config.AuthType == "jwt" ? $"Bearer {config.AuthToken}" : $"ApiKey {config.AuthToken}");
        }

        // Add custom headers
        foreach (var (key, value) in config.Headers)
        {
            _httpClient.DefaultRequestHeaders.Add(key, value);
        }
    }

    /// <inheritdoc />
    public async Task<McpListToolsResponse> ListToolsAsync(
        McpListToolsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing tools from service {ServiceName}", ServiceName);

        try
        {
            var response = await _httpClient.GetAsync("/mcp/tools", cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<McpListToolsResponse>(
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                throw new InvalidOperationException("Service returned null response");
            }

            _logger.LogInformation(
                "Found {Count} tools from {ServiceName}",
                result.Tools.Count,
                ServiceName);

            // Tag tools with service name
            var taggedTools = result.Tools.Select(tool =>
                tool.ServiceName == null
                    ? tool with { ServiceName = ServiceName }
                    : tool
            ).ToList();

            return result with { Tools = taggedTools };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to list tools from {ServiceName}", ServiceName);
            throw new McpException($"Failed to connect to {ServiceName}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse response from {ServiceName}", ServiceName);
            throw new McpException($"Invalid response from {ServiceName}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<McpToolCallResponse> CallToolAsync(
        McpToolCallRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Calling tool {Tool} on service {ServiceName}",
            request.Tool,
            ServiceName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync(
                "/mcp/tools/call",
                request,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            if (!httpResponse.IsSuccessStatusCode)
            {
                var error = await httpResponse.Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Compatibility fallback: some MCP servers require "name" instead of "tool".
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity &&
                    error.Contains("\"name\"", StringComparison.OrdinalIgnoreCase) &&
                    error.Contains("Field required", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "Retrying tool call {Tool} on {ServiceName} with name-based MCP payload",
                        request.Tool,
                        ServiceName);

                    var compatPayload = new
                    {
                        name = request.Tool,
                        arguments = request.Arguments,
                        correlationId = request.CorrelationId,
                        timeoutSeconds = request.TimeoutSeconds
                    };

                    var compatResponse = await _httpClient.PostAsJsonAsync(
                        "/mcp/tools/call",
                        compatPayload,
                        _jsonOptions,
                        cancellationToken).ConfigureAwait(false);

                    stopwatch.Stop();

                    if (compatResponse.IsSuccessStatusCode)
                    {
                        var compatResult = await compatResponse.Content.ReadFromJsonAsync<McpToolCallResponse>(
                            _jsonOptions,
                            cancellationToken).ConfigureAwait(false);
                        if (compatResult != null)
                        {
                            _logger.LogInformation(
                                "Compatibility tool call completed: {Tool} in {Duration}ms (success: {Success})",
                                request.Tool,
                                stopwatch.ElapsedMilliseconds,
                                compatResult.Success);
                            return compatResult with { DurationMs = stopwatch.ElapsedMilliseconds };
                        }
                    }

                    error = await compatResponse.Content.ReadAsStringAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                _logger.LogWarning(
                    "Tool call failed: {StatusCode} - {Error}",
                    httpResponse.StatusCode,
                    error);

                return new McpToolCallResponse
                {
                    Success = false,
                    Error = error,
                    ErrorCode = httpResponse.StatusCode.ToString(),
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }

            var result = await httpResponse.Content.ReadFromJsonAsync<McpToolCallResponse>(
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                throw new InvalidOperationException("Service returned null response");
            }

            _logger.LogInformation(
                "Tool call completed: {Tool} in {Duration}ms (success: {Success})",
                request.Tool,
                stopwatch.ElapsedMilliseconds,
                result.Success);

            return result with { DurationMs = stopwatch.ElapsedMilliseconds };
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to call tool {Tool} on {ServiceName}", request.Tool, ServiceName);

            return new McpToolCallResponse
            {
                Success = false,
                Error = ex.Message,
                ErrorCode = "CONNECTION_ERROR",
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error calling tool {Tool}", request.Tool);

            return new McpToolCallResponse
            {
                Success = false,
                Error = ex.Message,
                ErrorCode = "UNKNOWN_ERROR",
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <inheritdoc />
    public async Task<McpServiceHealth> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking health of {ServiceName}", ServiceName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken)
                .ConfigureAwait(false);
            stopwatch.Stop();

            var status = response.IsSuccessStatusCode ? "healthy" : "degraded";

            return new McpServiceHealth
            {
                ServiceName = ServiceName,
                Status = status,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                LastCheck = DateTime.UtcNow,
                LastSuccess = response.IsSuccessStatusCode ? DateTime.UtcNow : null,
                ErrorRate = 0.0,
                AvailableTools = 0
            };
        }
        catch
        {
            stopwatch.Stop();
            return new McpServiceHealth
            {
                ServiceName = ServiceName,
                Status = "down",
                LatencyMs = stopwatch.ElapsedMilliseconds,
                LastCheck = DateTime.UtcNow,
                LastSuccess = null,
                ErrorRate = 1.0,
                AvailableTools = 0
            };
        }
    }

    /// <inheritdoc />
    public async Task<McpToolDefinition?> GetToolAsync(
        string toolName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var tools = await ListToolsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return tools.Tools.FirstOrDefault(t => t.Name == toolName);
    }
}
