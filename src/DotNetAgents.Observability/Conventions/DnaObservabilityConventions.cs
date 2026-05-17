using System.Diagnostics;
using DotNetAgents.Observability.LaneOps;

namespace DotNetAgents.Observability.Conventions;

/// <summary>
/// Canonical DotNetAgents observability vocabulary for traces, metrics, structured
/// logs, and redaction posture. Service-specific telemetry should prefer these
/// names before introducing local variants.
/// </summary>
public static class DnaObservabilityConventions
{
    /// <summary>Stable framework label used on spans and logs emitted by DotNetAgents.</summary>
    public const string FrameworkName = "dotnetagents";

    /// <summary>ActivitySource names that baseline DotNetAgents hosts should subscribe to.</summary>
    public static class ActivitySources
    {
        public const string Core = "DotNetAgents.Core";
        public const string CoreChains = "DotNetAgents.Core.Chains";
        public const string Workflow = "DotNetAgents.Workflow";
        public const string Observability = "DotNetAgents.Observability";
        public const string Agents = "DotNetAgents.Agents";
        public const string LaneOps = LaneTracingExtensions.SourceName;

        public static readonly IReadOnlyList<string> DotNetAgentsFramework =
        [
            Core,
            CoreChains,
            Workflow,
            Observability,
            Agents,
            LaneOps,
        ];
    }

    /// <summary>Stable span names for common DotNetAgents operations.</summary>
    public static class SpanNames
    {
        public const string AgentExecute = "agent.execute";
        public const string A2ACall = "a2a.call";
        public const string McpCall = "mcp.call";
        public const string ModelInvoke = "llm.call";
        public const string ToolExecute = "tool.execute";
        public const string WorkflowExecute = "workflow.execute";
        public const string StateMachineTransition = "state_machine.transition";
        public const string StorageCall = "storage.call";
        public const string VectorCall = "vector.call";
    }

    /// <summary>Stable tag and structured-log field names for platform correlation.</summary>
    public static class Attributes
    {
        public const string ActorId = "dna.actor.id";
        public const string StoryId = "dna.story.id";
        public const string Lane = "dna.lane";
        public const string CorrelationId = "correlation.id";
        public const string ToolName = "tool.name";
        public const string ServiceName = "service.name";
        public const string ModelId = "llm.model";
        public const string ProviderId = "dna.provider.id";
        public const string Route = "dna.route";
        public const string CostUsd = "dna.cost.usd";
        public const string LatencyMs = "dna.latency_ms";
        public const string Outcome = "dna.outcome";
        public const string ErrorCode = "error.code";
        public const string WorkflowName = "workflow.name";
        public const string WorkflowRunId = "workflow.run_id";
    }

    /// <summary>Stable metric instrument names for invocation, latency, cost, and resilience.</summary>
    public static class Metrics
    {
        public const string InvocationCount = "dna.invocations";
        public const string InvocationLatencyMs = "dna.invocation.latency_ms";
        public const string InvocationCostUsd = "dna.invocation.cost_usd";
        public const string ErrorCount = "dna.errors";
        public const string RetryCount = "dna.retries";
        public const string CircuitBreakerEvents = "dna.circuit_breaker.events";
    }

    /// <summary>Payload classes that must be referenced by hash/id instead of raw value.</summary>
    public static class Redaction
    {
        public const string PromptPayload = "prompt_payload";
        public const string ModelOutput = "model_output";
        public const string CredentialValue = "credential_value";
        public const string FilePath = "file_path";
        public const string PersonalData = "personal_data";
    }

    /// <summary>Known outcome values for spans, logs, and metrics.</summary>
    public static class Outcomes
    {
        public const string Success = "success";
        public const string Failure = "failure";
        public const string Cancelled = "cancelled";
        public const string Degraded = "degraded";
        public const string Skipped = "skipped";
    }

    /// <summary>
    /// Stamp optional shared correlation fields on an activity. Values are ignored when
    /// null or whitespace so callers can pass partially-known runtime context safely.
    /// </summary>
    public static Activity? AddDnaCorrelationTags(
        this Activity? activity,
        string? actorId = null,
        string? storyId = null,
        string? lane = null,
        string? correlationId = null,
        string? serviceName = null,
        string? outcome = null,
        string? errorCode = null)
    {
        if (activity is null)
        {
            return null;
        }

        SetIfPresent(activity, Attributes.ActorId, actorId);
        SetIfPresent(activity, Attributes.StoryId, storyId);
        SetIfPresent(activity, Attributes.Lane, lane);
        SetIfPresent(activity, Attributes.CorrelationId, correlationId);
        SetIfPresent(activity, Attributes.ServiceName, serviceName);
        SetIfPresent(activity, Attributes.Outcome, outcome);
        SetIfPresent(activity, Attributes.ErrorCode, errorCode);
        return activity;
    }

    private static void SetIfPresent(Activity activity, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            activity.SetTag(key, value);
        }
    }
}
