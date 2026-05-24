// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Server;

internal static class McpObservabilityPublisher
{
    public static Task PublishToolCallAsync(
        IDnaObservabilityPublisher publisher,
        string serviceName,
        string toolName,
        string correlationId,
        IReadOnlyDictionary<string, object> arguments,
        McpToolCallResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(response);

        var outcome = ResolveOutcome(response);
        return publisher.PublishAsync(
            new DnaObservabilityEnvelopeRequest(
                Topic: "dna.mcp.tool_call",
                EventType: ResolveEventType(outcome),
                SourceService: serviceName)
            {
                Severity = outcome is "failed" ? "warning" : "info",
                CorrelationId = correlationId,
                StoryId = FindValue(arguments, "storyId", "story_id", "sdlc.story_id"),
                RunId = FindValue(arguments, "runId", "run_id"),
                WorkflowRunId = FindValue(arguments, "workflowRunId", "workflow_run_id"),
                SubjectKind = "mcp_tool_call",
                SubjectId = correlationId,
                SubjectName = toolName,
                PayloadSummary = BuildPayloadSummary(toolName, outcome, response),
                PrivacyClass = "confidential",
                RetentionClass = "audit",
                RedactionStatus = "redacted",
                RedactionRules = ["mcp.arguments.redacted", "mcp.result.redacted"],
                Tags = ["mcp", toolName, outcome],
                Dimensions = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["mcp.tool"] = toolName,
                    ["mcp.outcome"] = outcome,
                    ["policy.risk_class"] = ResolveRiskClass(response.Metadata)
                },
                Metrics = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["mcp.duration_ms"] = response.DurationMs,
                    ["mcp.success"] = response.Success ? 1 : 0
                }
            },
            cancellationToken);
    }

    private static string ResolveOutcome(McpToolCallResponse response)
    {
        if (string.Equals(response.ErrorCode, "FORBIDDEN", StringComparison.OrdinalIgnoreCase))
        {
            return "denied";
        }

        return response.Success ? "executed" : "failed";
    }

    private static string ResolveEventType(string outcome)
        => outcome switch
        {
            "denied" => "mcp.tool.denied",
            "failed" => "mcp.tool.failed",
            _ => "mcp.tool.executed"
        };

    private static string ResolveRiskClass(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return "unknown";
        }

        if (metadata.TryGetValue("policy.risk_class", out var direct) && !string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        if (metadata.TryGetValue("guardrailRiskTier", out var guardrailRiskTier) && !string.IsNullOrWhiteSpace(guardrailRiskTier))
        {
            return guardrailRiskTier;
        }

        if (metadata.TryGetValue("riskTier", out var riskTier) && !string.IsNullOrWhiteSpace(riskTier))
        {
            return riskTier;
        }

        return "unknown";
    }

    private static string BuildPayloadSummary(string toolName, string outcome, McpToolCallResponse response)
    {
        return outcome switch
        {
            "denied" => $"MCP tool {toolName} was denied by policy.",
            "failed" => $"MCP tool {toolName} failed with {response.ErrorCode ?? "UNKNOWN"}.",
            _ => $"MCP tool {toolName} executed successfully."
        };
    }

    private static string? FindValue(IReadOnlyDictionary<string, object> arguments, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!arguments.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            var text = value.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }
}
