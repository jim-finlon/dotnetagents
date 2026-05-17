using System.Text.Json;
using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp.Server;

/// <summary>
/// Wraps MCP tool calls with lesson.event.v1 capture and projection.
/// </summary>
public sealed class McpLearningDecorator(
    IMcpToolProvider inner,
    IAgentLearningProjector projector,
    ILogger<McpLearningDecorator> logger,
    McpLearningDecoratorOptions options) : IMcpToolProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly object LogSync = new();

    public Task<IReadOnlyList<McpToolDefinition>> GetToolsAsync(string serviceName, CancellationToken cancellationToken = default)
        => inner.GetToolsAsync(serviceName, cancellationToken);

    public async Task<McpToolCallResponse> CallToolAsync(string toolName, IReadOnlyDictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        try
        {
            var response = await inner.CallToolAsync(toolName, arguments, cancellationToken).ConfigureAwait(false);
            await PublishEventAsync(toolName, started, response, exception: null, cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            await PublishEventAsync(toolName, started, response: null, ex, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task PublishEventAsync(
        string toolName,
        DateTimeOffset started,
        McpToolCallResponse? response,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        ValidateOptions();

        var durationMs = (long)Math.Max((DateTimeOffset.UtcNow - started).TotalMilliseconds, 0);
        var success = exception is null && response?.Success != false;
        var outcome = success ? "success" : "failure";
        var errorCode = exception?.GetType().Name ?? response?.ErrorCode;
        var summary = exception?.Message ?? response?.Summary ?? response?.Error ?? $"{toolName} completed.";

        var learningEvent = new LearningEventV1
        {
            CorrelationId = Guid.NewGuid().ToString("N"),
            Service = options.Service,
            Project = options.ProjectName,
            ActorType = options.ActorType,
            ActorId = options.ActorId,
            WorkflowId = toolName,
            Step = "mcp.tools.call",
            Intent = toolName,
            Outcome = outcome,
            Confidence = success ? options.SuccessConfidence : options.FailureConfidence,
            TimeCostMs = durationMs,
            ProblemSignature = success
                ? $"mcp:{options.Service}:{toolName}:success"
                : $"mcp:{options.Service}:{toolName}:failure:{errorCode ?? "unknown"}",
            LessonSummary = summary,
            ErrorCode = errorCode,
            ErrorMessage = exception?.Message ?? response?.Error,
            Tags = [options.Service, "mcp", toolName, outcome]
        };

        AppendLocal(learningEvent);
        await projector.ProjectAsync(learningEvent, cancellationToken).ConfigureAwait(false);
    }

    private void AppendLocal(LearningEventV1 learningEvent)
    {
        try
        {
            var fullPath = Path.GetFullPath(options.EventLogPath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = JsonSerializer.Serialize(learningEvent, JsonOptions);
            lock (LogSync)
            {
                File.AppendAllText(fullPath, line + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to append MCP learning event for service {Service}.", options.Service);
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(options.Service))
        {
            throw new InvalidOperationException("McpLearningDecoratorOptions.Service is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ProjectName))
        {
            throw new InvalidOperationException("McpLearningDecoratorOptions.ProjectName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ActorId))
        {
            throw new InvalidOperationException("McpLearningDecoratorOptions.ActorId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.EventLogPath))
        {
            throw new InvalidOperationException("McpLearningDecoratorOptions.EventLogPath is required.");
        }
    }
}
