// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using DotNetAgents.Mcp.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp.Server;

/// <summary>
/// Maps MCP tool endpoints (GET /mcp/tools, POST /mcp/tools/call) so the DotNetAgents.Mcp client can discover and call tools.
/// </summary>
public static class McpServerExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Maps GET /mcp/tools and POST /mcp/tools/call using the registered <see cref="IMcpToolProvider"/>.
    /// Optionally maps GET /health returning 200 with service name.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="serviceName">Service name to tag tool definitions and use in health (e.g. "time_management").</param>
    /// <param name="mapHealth">If true, maps GET /health returning { "status": "healthy", "serviceName": "..." }.</param>
    /// <param name="instructionsBootstrap">If set, maps GET /mcp/instructions with this JSON (DNA bootstrap contract).</param>
    /// <returns>The route group for further configuration.</returns>
    public static IEndpointRouteBuilder MapMcpEndpoints(
        this IEndpointRouteBuilder endpoints,
        string serviceName,
        bool mapHealth = true,
        McpInstructionsResponse? instructionsBootstrap = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var group = endpoints.MapGroup("/mcp").WithTags("MCP");

        if (instructionsBootstrap != null)
        {
            var instructions = instructionsBootstrap;
            group.MapGet("/instructions", () => Results.Json(instructions, JsonOptions))
                .WithName("GetMcpInstructions")
                .WithDisplayName("MCP bootstrap instructions");
        }

        group.MapGet("/tools", async (string? category, int? limit, IMcpToolProvider provider, CancellationToken ct) =>
        {
            var tools = await provider.GetToolsAsync(serviceName, ct).ConfigureAwait(false);
            var tagged = tools.Select(t => t.ServiceName == null ? t with { ServiceName = serviceName } : t).ToList();
            var filtered = tagged.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(category))
                filtered = filtered.Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));
            var list = filtered.ToList();
            if (limit is > 0)
                list = list.Take(limit.Value).ToList();
            return Results.Json(new McpListToolsResponse { Tools = list, TotalCount = list.Count }, JsonOptions);
        })
            .WithName("ListMcpTools")
            .WithDisplayName("List MCP tools")
            .Produces<McpListToolsResponse>(StatusCodes.Status200OK);

        group.MapPost("/tools/call", async (McpToolCallRequest request, IMcpToolProvider provider, ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Tool))
                return Results.BadRequest(new McpToolCallResponse { Success = false, Error = "Tool name is required", ErrorCode = "INVALID_REQUEST" });

            var logger = loggerFactory.CreateLogger("McpServer");
            var args = request.Arguments ?? new Dictionary<string, object>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId;
            var startedAtUtc = DateTimeOffset.UtcNow;
            var eventSink = endpoints.ServiceProvider.GetService<IMcpEventSink>() ?? NoOpMcpEventSink.Instance;
            var verifier = endpoints.ServiceProvider.GetService<IMcpSafetyVerifier>() ?? NoOpMcpSafetyVerifier.Instance;
            var lessonPublisher = endpoints.ServiceProvider.GetService<ILessonEventPublisher>() ?? NoOpLessonEventPublisher.Instance;
            var observabilityPublisher = endpoints.ServiceProvider.GetService<IDnaObservabilityPublisher>() ?? NoOpDnaObservabilityPublisher.Instance;
            var context = new McpToolCallContext(serviceName, request.Tool, args, correlationId!, startedAtUtc);

            await eventSink.OnToolCallStartedAsync(context, ct).ConfigureAwait(false);

            var beforeCheck = await verifier.VerifyBeforeCallAsync(context, ct).ConfigureAwait(false);
            if (!beforeCheck.Allowed)
            {
                stopwatch.Stop();
                var blockedResponse = EnrichMetadata(new McpToolCallResponse
                {
                    Success = false,
                    Error = beforeCheck.Message ?? "Tool execution blocked by safety verifier",
                    ErrorCode = beforeCheck.ErrorCode ?? "FORBIDDEN",
                    DurationMs = stopwatch.ElapsedMilliseconds
                }, serviceName, request.Tool, correlationId!, beforeCheck.Metadata);

                var blockedEvent = new McpToolLifecycleEvent(
                    serviceName,
                    request.Tool,
                    correlationId!,
                    false,
                    blockedResponse.DurationMs,
                    blockedResponse.ErrorCode,
                    blockedResponse.Error);
                await eventSink.OnToolCallCompletedAsync(context, blockedResponse, blockedEvent, ct).ConfigureAwait(false);
                await PublishLessonAsync(lessonPublisher, serviceName, request.Tool, correlationId!, blockedResponse, "blocked_by_safety", ct).ConfigureAwait(false);
                await McpObservabilityPublisher.PublishToolCallAsync(
                    observabilityPublisher,
                    serviceName,
                    request.Tool,
                    correlationId!,
                    args,
                    blockedResponse,
                    ct).ConfigureAwait(false);
                return Results.Json(blockedResponse, JsonOptions);
            }

            try
            {
                var catalog = await provider.GetToolsAsync(serviceName, ct).ConfigureAwait(false);
                var definition = catalog.FirstOrDefault(t =>
                    string.Equals(t.Name, request.Tool, StringComparison.OrdinalIgnoreCase));
                var missingArgs = McpRequiredArgumentGuard.TryGetMissingArgumentResponse(
                    definition,
                    request.Tool,
                    args);
                var response = missingArgs
                    ?? await provider.CallToolAsync(request.Tool, args, ct).ConfigureAwait(false);
                stopwatch.Stop();
                var enrichedResponse = EnrichMetadata(
                    response with { DurationMs = stopwatch.ElapsedMilliseconds },
                    serviceName,
                    request.Tool,
                    correlationId!,
                    beforeCheck.Metadata);
                var afterCheck = await verifier.VerifyAfterCallAsync(context, enrichedResponse, ct).ConfigureAwait(false);
                enrichedResponse = enrichedResponse with
                {
                    Metadata = MergeMetadata(enrichedResponse.Metadata, afterCheck.Metadata)
                };
                if (!afterCheck.Allowed)
                {
                    enrichedResponse = enrichedResponse with
                    {
                        Success = false,
                        Error = afterCheck.Message ?? "Tool output blocked by safety verifier",
                        ErrorCode = afterCheck.ErrorCode ?? "FORBIDDEN"
                    };
                }

                var lifecycleEvent = new McpToolLifecycleEvent(
                    serviceName,
                    request.Tool,
                    correlationId!,
                    enrichedResponse.Success,
                    enrichedResponse.DurationMs,
                    enrichedResponse.ErrorCode,
                    enrichedResponse.Error);
                await eventSink.OnToolCallCompletedAsync(context, enrichedResponse, lifecycleEvent, ct).ConfigureAwait(false);
                await PublishLessonAsync(
                    lessonPublisher,
                    serviceName,
                    request.Tool,
                    correlationId!,
                    enrichedResponse,
                    enrichedResponse.Success ? "completed" : "failed",
                    ct).ConfigureAwait(false);
                await McpObservabilityPublisher.PublishToolCallAsync(
                    observabilityPublisher,
                    serviceName,
                    request.Tool,
                    correlationId!,
                    args,
                    enrichedResponse,
                    ct).ConfigureAwait(false);
                return Results.Json(enrichedResponse, JsonOptions);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                var response = EnrichMetadata(new McpToolCallResponse
                {
                    Success = false,
                    Error = "Request was cancelled",
                    ErrorCode = "CANCELLED",
                    DurationMs = stopwatch.ElapsedMilliseconds
                }, serviceName, request.Tool, correlationId!, beforeCheck.Metadata);
                var lifecycleEvent = new McpToolLifecycleEvent(
                    serviceName,
                    request.Tool,
                    correlationId!,
                    false,
                    response.DurationMs,
                    response.ErrorCode,
                    response.Error);
                await eventSink.OnToolCallCompletedAsync(context, response, lifecycleEvent, CancellationToken.None).ConfigureAwait(false);
                await PublishLessonAsync(lessonPublisher, serviceName, request.Tool, correlationId!, response, "cancelled", CancellationToken.None).ConfigureAwait(false);
                await McpObservabilityPublisher.PublishToolCallAsync(
                    observabilityPublisher,
                    serviceName,
                    request.Tool,
                    correlationId!,
                    args,
                    response,
                    CancellationToken.None).ConfigureAwait(false);
                return Results.Json(response, JsonOptions);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "Tool call failed: {Tool}", request.Tool);
                var response = EnrichMetadata(new McpToolCallResponse
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = "TOOL_ERROR",
                    DurationMs = stopwatch.ElapsedMilliseconds
                }, serviceName, request.Tool, correlationId!, beforeCheck.Metadata);
                var lifecycleEvent = new McpToolLifecycleEvent(
                    serviceName,
                    request.Tool,
                    correlationId!,
                    false,
                    response.DurationMs,
                    response.ErrorCode,
                    response.Error);
                await eventSink.OnToolCallCompletedAsync(context, response, lifecycleEvent, CancellationToken.None).ConfigureAwait(false);
                await PublishLessonAsync(lessonPublisher, serviceName, request.Tool, correlationId!, response, "exception", CancellationToken.None).ConfigureAwait(false);
                await McpObservabilityPublisher.PublishToolCallAsync(
                    observabilityPublisher,
                    serviceName,
                    request.Tool,
                    correlationId!,
                    args,
                    response,
                    CancellationToken.None).ConfigureAwait(false);
                return Results.Json(response, JsonOptions);
            }
        })
            .DisableAntiforgery()
            .WithName("CallMcpTool")
            .WithDisplayName("Call MCP tool")
            .Accepts<McpToolCallRequest>("application/json")
            .Produces<McpToolCallResponse>(StatusCodes.Status200OK)
            .Produces<McpToolCallResponse>(StatusCodes.Status400BadRequest);

        if (mapHealth)
        {
            endpoints.MapGet("/health", () => Results.Json(new
            {
                status = "healthy",
                serviceName
            }, JsonOptions))
                .WithName("Health")
                .WithDisplayName("Health check")
                .ExcludeFromDescription();
        }

        return endpoints;
    }

    private static McpToolCallResponse EnrichMetadata(
        McpToolCallResponse response,
        string service,
        string tool,
        string correlationId,
        IReadOnlyDictionary<string, string>? extraMetadata = null)
    {
        var metadata = new Dictionary<string, string>(response.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["service"] = service,
            ["tool"] = tool,
            ["correlationId"] = correlationId
        };
        if (extraMetadata is not null)
        {
            foreach (var kvp in extraMetadata)
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }

        var remediation = response.Success
            ? null
            : EnrichRemediation(response, service, tool, metadata);

        return response with { Metadata = metadata, Remediation = remediation };
    }

    private static McpRemediation EnrichRemediation(
        McpToolCallResponse response,
        string service,
        string tool,
        IReadOnlyDictionary<string, string> metadata)
    {
        var existing = response.Remediation;
        var remediationMetadata = new Dictionary<string, string>(
            existing?.Metadata ?? new Dictionary<string, string>(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var key in new[] { "validationKind", "invalidArgument", "failedCapability", "correlationId" })
        {
            if (metadata.TryGetValue(key, out var value))
                remediationMetadata[key] = value;
        }

        return new McpRemediation
        {
            RemediationKind = existing?.RemediationKind ?? InferRemediationKind(response.ErrorCode),
            ServiceName = existing?.ServiceName ?? service,
            ToolName = existing?.ToolName ?? tool,
            ErrorCode = existing?.ErrorCode ?? response.ErrorCode,
            InvalidArgument = existing?.InvalidArgument ?? metadata.GetValueOrDefault("invalidArgument"),
            FailedCapability = existing?.FailedCapability ?? metadata.GetValueOrDefault("failedCapability"),
            Guidance = existing?.Guidance ?? response.Guidance,
            SuggestedNextSteps = existing?.SuggestedNextSteps.Count > 0
                ? existing.SuggestedNextSteps
                : response.SuggestedNextSteps ?? Array.Empty<string>(),
            Metadata = remediationMetadata
        };
    }

    private static string InferRemediationKind(string? errorCode) =>
        errorCode?.ToUpperInvariant() switch
        {
            "MISSING_ARG" or "INVALID_REQUEST" or "ARGUMENT_VALIDATION_ERROR" or "VALIDATION_ERROR" => "validation",
            "UNAUTHORIZED" => "auth",
            "FORBIDDEN" => "permission",
            "CANCELLED" => "runtime",
            _ => "workflow"
        };

    private static Dictionary<string, string> MergeMetadata(
        IReadOnlyDictionary<string, string>? existing,
        IReadOnlyDictionary<string, string>? extra)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (existing is not null)
        {
            foreach (var kvp in existing)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        if (extra is not null)
        {
            foreach (var kvp in extra)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        return merged;
    }

    private static async Task PublishLessonAsync(
        ILessonEventPublisher lessonPublisher,
        string service,
        string tool,
        string correlationId,
        McpToolCallResponse response,
        string outcome,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;
        var problemSignature = response.Success
            ? null
            : $"{service}:{tool}:{response.ErrorCode ?? "UNKNOWN"}";
        var lessonSummary = response.Success
            ? $"Tool {tool} completed successfully."
            : $"Tool {tool} failed with {response.ErrorCode ?? "UNKNOWN"}.";
        var confidence = response.Success ? 0.9 : 0.3;

        var lesson = new LessonEvent(
            EventId: eventId,
            OccurredAtUtc: occurredAt,
            CorrelationId: correlationId,
            Service: service,
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
            SourceService = service,
            Service = service,
            WorkflowId = "mcp.tools.call",
            Step = tool,
            Intent = tool,
            Outcome = outcome,
            DurationMs = response.DurationMs,
            TimeCostMs = response.DurationMs,
            ProblemSignature = problemSignature ?? string.Empty,
            LessonSummary = lessonSummary,
            ErrorCode = response.ErrorCode,
            ErrorMessage = response.Error,
            Confidence = confidence
        };

        // Fan out to both contracts: older LessonEvent implementers keep working; newer
        // implementers opt in to the richer dna.learning.event.v1 envelope by overriding
        // PublishLearningEventAsync.
        await lessonPublisher.PublishAsync(lesson, cancellationToken).ConfigureAwait(false);
        await lessonPublisher.PublishLearningEventAsync(learningEvent, cancellationToken).ConfigureAwait(false);
    }
}
