// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetAgents.Mcp.Models;
using DotNetAgents.Mcp.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetAgents.Mcp.Stdio;

/// <summary>
/// Reusable JSON-RPC 2.0 MCP server over stdio. Any DNA service that implements
/// <see cref="IMcpToolProvider"/> can expose it on stdin/stdout without rolling its own
/// protocol loop — this class mirrors the method dispatch of
/// <see cref="McpStreamableHttpExtensions.MapMcpStreamableHttp"/> but over line-delimited
/// JSON messages instead of HTTP.
///
/// Tool calls auto-emit the canonical <c>dna.learning.event.v1</c> envelope through the
/// registered <see cref="ILessonEventPublisher"/>, matching the HTTP paths (see A3 Phases
/// 1-2 in DotNetAgents.Mcp.Server). The stdio workflow id is <c>mcp.stdio.tools.call</c>.
///
/// This server is deliberately minimal: it handles <c>initialize</c>, <c>tools/list</c>,
/// and <c>tools/call</c>. Higher-level capabilities (<c>resources/</c>, <c>prompts/</c>,
/// session management) are out of scope for v1 and may be added as follow-ups when a
/// concrete use case demands them.
/// </summary>
public sealed class McpStdioServer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly string _serviceName;
    private readonly string _serverDisplayName;
    private readonly string _serverVersion;
    private readonly IMcpToolProvider _provider;
    private readonly ILessonEventPublisher _lessonPublisher;
    private readonly ILogger _logger;

    public McpStdioServer(
        string serviceName,
        string serverDisplayName,
        string serverVersion,
        IMcpToolProvider provider,
        ILessonEventPublisher? lessonPublisher = null,
        ILogger<McpStdioServer>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverVersion);
        ArgumentNullException.ThrowIfNull(provider);

        _serviceName = serviceName;
        _serverDisplayName = serverDisplayName;
        _serverVersion = serverVersion;
        _provider = provider;
        _lessonPublisher = lessonPublisher ?? NoOpLessonEventPublisher.Instance;
        _logger = logger ?? NullLogger<McpStdioServer>.Instance;
    }

    /// <summary>
    /// Runs the stdio read/dispatch loop until <paramref name="cancellationToken"/> is cancelled
    /// or <paramref name="input"/> reaches EOF. Reads one JSON-RPC message per line from
    /// <paramref name="input"/> and writes one response line to <paramref name="output"/>.
    /// Notifications (requests without an id) produce no output.
    /// </summary>
    public async Task RunAsync(
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (line is null)
            {
                // EOF — remote end closed stdin.
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var response = await ProcessMessageAsync(line, cancellationToken).ConfigureAwait(false);
            if (response is null)
            {
                // Notification — nothing to write.
                continue;
            }

            await output.WriteLineAsync(response.AsMemory(), cancellationToken).ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<string?> ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        JsonDocument? doc;
        try
        {
            doc = JsonDocument.Parse(message);
        }
        catch (JsonException)
        {
            return BuildError(requestId: null, code: -32700, messageText: "Parse error");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (!root.TryGetProperty("jsonrpc", out var jv) || jv.GetString() != "2.0")
            {
                return BuildError(GetRequestId(root), -32600, "Invalid JSON-RPC");
            }
            if (!root.TryGetProperty("method", out var methodEl) || methodEl.ValueKind != JsonValueKind.String)
            {
                return BuildError(GetRequestId(root), -32600, "Missing method");
            }

            var method = methodEl.GetString();
            if (string.IsNullOrEmpty(method))
            {
                return BuildError(GetRequestId(root), -32600, "Invalid method");
            }

            var hasId = root.TryGetProperty("id", out var idElement);
            JsonElement? id = hasId ? idElement : null;
            root.TryGetProperty("params", out var paramsElement);

            if (!hasId)
            {
                // Notifications — e.g. notifications/initialized — are fire-and-forget.
                return null;
            }

            return method switch
            {
                "initialize" => BuildInitializeResponse(id!.Value),
                "ping" => BuildResult(id!.Value, new JsonObject()),
                "tools/list" => await BuildToolsListResponseAsync(id!.Value, cancellationToken).ConfigureAwait(false),
                "tools/call" => await BuildToolsCallResponseAsync(id!.Value, paramsElement, cancellationToken).ConfigureAwait(false),
                _ => BuildError(id!.Value, -32601, $"Method not found: {method}")
            };
        }
    }

    private string BuildInitializeResponse(JsonElement id)
    {
        var result = new JsonObject
        {
            ["protocolVersion"] = "2025-11-25",
            ["serverInfo"] = new JsonObject
            {
                ["name"] = _serverDisplayName,
                ["version"] = _serverVersion
            },
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject
                {
                    ["listChanged"] = false
                }
            }
        };
        return BuildResult(id, result);
    }

    private async Task<string> BuildToolsListResponseAsync(JsonElement id, CancellationToken cancellationToken)
    {
        var tools = await _provider.GetToolsAsync(_serviceName, cancellationToken).ConfigureAwait(false);
        var array = new JsonArray();
        foreach (var tool in tools)
        {
            // Framework-enforced naming (see DotNetAgents.Mcp.Models.McpToolNameConvention)
            // already rejected invalid names at registration. Emit as-registered.
            var input = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = JsonSerializer.SerializeToNode(tool.InputSchema.Properties, JsonOptions),
                ["required"] = JsonSerializer.SerializeToNode(tool.InputSchema.Required, JsonOptions)
            };
            array.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["inputSchema"] = input
            });
        }

        return BuildResult(id, new JsonObject { ["tools"] = array });
    }

    private async Task<string> BuildToolsCallResponseAsync(
        JsonElement id,
        JsonElement paramsElement,
        CancellationToken cancellationToken)
    {
        if (paramsElement.ValueKind == JsonValueKind.Undefined ||
            !paramsElement.TryGetProperty("name", out var nameEl))
        {
            return BuildError(id, -32602, "Invalid params: tools/call requires name");
        }

        var toolName = nameEl.GetString();
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return BuildError(id, -32602, "Invalid params: name");
        }

        // Migration-compat normalization matching the HTTP streamable path.
        toolName = McpToolNameConvention.Normalize(toolName);

        var args = new Dictionary<string, object>();
        if (paramsElement.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in argsEl.EnumerateObject())
            {
                args[p.Name] = JsonElementToArg(p.Value);
            }
        }

        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString("N");
        try
        {
            var response = await _provider.CallToolAsync(toolName!, args, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            await PublishLearningAsync(toolName!, correlationId, response, stopwatch.ElapsedMilliseconds, cancellationToken)
                .ConfigureAwait(false);

            var result = BuildCallToolResult(response);
            return BuildResult(id, result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "stdio tools/call failed for {Tool}", toolName);
            var exceptionResponse = new McpToolCallResponse
            {
                Success = false,
                Error = ex.Message,
                ErrorCode = "TOOL_ERROR",
                DurationMs = stopwatch.ElapsedMilliseconds
            };
            await PublishLearningAsync(toolName!, correlationId, exceptionResponse, stopwatch.ElapsedMilliseconds, CancellationToken.None)
                .ConfigureAwait(false);

            var result = BuildCallToolResult(exceptionResponse);
            return BuildResult(id, result);
        }
    }

    private async Task PublishLearningAsync(
        string tool,
        string correlationId,
        McpToolCallResponse response,
        long durationMs,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;
        var outcome = response.Success ? "completed" : "failed";
        var problemSignature = response.Success
            ? null
            : $"{_serviceName}:{tool}:{response.ErrorCode ?? "UNKNOWN"}";
        var lessonSummary = response.Success
            ? $"Tool {tool} completed successfully."
            : $"Tool {tool} failed with {response.ErrorCode ?? "UNKNOWN"}.";
        var confidence = response.Success ? 0.9 : 0.3;

        var lesson = new LessonEvent(
            EventId: eventId,
            OccurredAtUtc: occurredAt,
            CorrelationId: correlationId,
            Service: _serviceName,
            Step: tool,
            Outcome: outcome,
            LessonSummary: lessonSummary,
            ProblemSignature: problemSignature,
            ErrorCode: response.ErrorCode,
            Confidence: confidence);

        var learningEvent = new DnaLearningEvent
        {
            EventId = eventId,
            Timestamp = occurredAt,
            OccurredAtUtc = occurredAt,
            CorrelationId = correlationId,
            SourceService = _serviceName,
            Service = _serviceName,
            WorkflowId = "mcp.stdio.tools.call",
            Step = tool,
            Intent = tool,
            Outcome = outcome,
            DurationMs = durationMs,
            TimeCostMs = durationMs,
            ProblemSignature = problemSignature ?? string.Empty,
            LessonSummary = lessonSummary,
            ErrorCode = response.ErrorCode,
            ErrorMessage = response.Error,
            Confidence = confidence
        };

        await _lessonPublisher.PublishAsync(lesson, cancellationToken).ConfigureAwait(false);
        await _lessonPublisher.PublishLearningEventAsync(learningEvent, cancellationToken).ConfigureAwait(false);
    }

    private static JsonObject BuildCallToolResult(McpToolCallResponse r)
    {
        var text = JsonSerializer.Serialize(r, JsonOptions);
        var contentArray = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "text",
                ["text"] = text
            }
        };
        var obj = new JsonObject
        {
            ["content"] = contentArray,
            ["isError"] = !r.Success
        };

        if (r.Result is not null)
        {
            obj["structuredContent"] = r.Result switch
            {
                JsonElement je => JsonSerializer.SerializeToNode(je, JsonOptions),
                _ => JsonSerializer.SerializeToNode(r.Result, JsonOptions)
            };
        }
        else if (!r.Success)
        {
            obj["structuredContent"] = JsonSerializer.SerializeToNode(new
            {
                r.Success,
                r.Error,
                r.ErrorCode
            }, JsonOptions);
        }

        return obj;
    }

    private string BuildResult(JsonElement id, JsonNode result)
    {
        var o = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = JsonNode.Parse(id.GetRawText()),
            ["result"] = result
        };
        return o.ToJsonString(JsonOptions);
    }

    private static string BuildError(JsonElement? requestId, int code, string messageText)
    {
        var idNode = requestId.HasValue ? JsonNode.Parse(requestId.Value.GetRawText()) : null;
        var o = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = idNode,
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = messageText
            }
        };
        return o.ToJsonString(JsonOptions);
    }

    private static JsonElement? GetRequestId(JsonElement root) =>
        root.TryGetProperty("id", out var id) ? id : null;

    private static object JsonElementToArg(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString()!,
        JsonValueKind.Number when el.TryGetInt64(out var i) => i,
        JsonValueKind.Number => el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null!,
        JsonValueKind.Object => el,
        JsonValueKind.Array => el,
        _ => el.GetRawText()
    };
}
