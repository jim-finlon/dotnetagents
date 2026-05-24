// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace DotNetAgents.Observability.GenAi;

/// <summary>Versioned payload contract for prompt-redacted DNA LLM call spans.</summary>
public sealed record LlmCallSpan(
    string CallId,
    DateTimeOffset ObservedAtUtc,
    string? AgentId,
    string Intent,
    string ModelId,
    string? PromptHash,
    LlmCallTokenUsage Tokens,
    long? LatencyMs,
    decimal? EstimatedCostUsd,
    string Outcome,
    string? StoryId = null,
    string? RunId = null,
    string? TraceId = null,
    string? SpanId = null,
    string? PromptArtifactId = null,
    string? ErrorCode = null,
    string? ProviderId = null,
    string? RoutingDecisionId = null,
    IReadOnlyDictionary<string, object?>? Metadata = null)
{
    public const string SchemaVersion = "dna.llm.call.v1";
}

/// <summary>Prompt, completion, and total token counts for <see cref="LlmCallSpan"/>.</summary>
public sealed record LlmCallTokenUsage(long? Prompt, long? Completion, long? Total);

/// <summary>Activity/event helpers for the <c>dna.llm.call.v1</c> span contract.</summary>
public static class LlmCallSpanTelemetry
{
    public const string ActivityEventName = LlmCallSpan.SchemaVersion;
    public const string ActivityTagPayloadKey = "dna.llm.call.payload";

    public static string HashPrompt(string prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(prompt));
        return "sha256:" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static void AttachToCurrentActivity(LlmCallSpan span)
    {
        ArgumentNullException.ThrowIfNull(span);
        var activity = Activity.Current;
        if (activity is null)
            return;

        var payload = ToDictionary(span);
        activity.SetTag(ActivityTagPayloadKey, LlmCallSpan.SchemaVersion);
        activity.SetTag("dna.llm.call.id", span.CallId);
        activity.SetTag("dna.llm.call.intent", span.Intent);
        activity.SetTag("dna.llm.call.model_id", span.ModelId);
        activity.SetTag("dna.llm.call.outcome", span.Outcome);
        activity.AddEvent(new ActivityEvent(ActivityEventName, tags: new ActivityTagsCollection(payload)));
    }

    public static IReadOnlyDictionary<string, object?> ToDictionary(LlmCallSpan span)
    {
        ArgumentNullException.ThrowIfNull(span);
        return new Dictionary<string, object?>
        {
            ["schemaVersion"] = LlmCallSpan.SchemaVersion,
            ["callId"] = span.CallId,
            ["traceId"] = span.TraceId,
            ["spanId"] = span.SpanId,
            ["observedAtUtc"] = span.ObservedAtUtc.ToUniversalTime().ToString("O"),
            ["agentId"] = span.AgentId,
            ["storyId"] = span.StoryId,
            ["runId"] = span.RunId,
            ["intent"] = span.Intent,
            ["modelId"] = span.ModelId,
            ["promptHash"] = span.PromptHash,
            ["promptArtifactId"] = span.PromptArtifactId,
            ["tokens.prompt"] = span.Tokens.Prompt,
            ["tokens.completion"] = span.Tokens.Completion,
            ["tokens.total"] = span.Tokens.Total,
            ["latencyMs"] = span.LatencyMs,
            ["estimatedCostUsd"] = span.EstimatedCostUsd is null ? null : (double)span.EstimatedCostUsd.Value,
            ["outcome"] = span.Outcome,
            ["errorCode"] = span.ErrorCode,
            ["providerId"] = span.ProviderId,
            ["routingDecisionId"] = span.RoutingDecisionId
        };
    }
}
