using System.Diagnostics;

namespace DotNetAgents.Observability.GenAi;

/// <summary>
/// Helpers that start <see cref="Activity"/> instances pre-stamped with OTEL GenAI semantic
/// convention attributes. Use these from agent/LLM/tool/framework call sites instead of
/// hand-rolling Activity tags — the helpers make sure attribute names match the spec
/// exactly so backends like Datadog, Langfuse, Braintrust render correctly.
/// </summary>
/// <remarks>
/// Returned <see cref="Activity"/> instances may be null when no listener is subscribed
/// to the activity source. Always wrap usage in <c>using var act = ...</c> — null is
/// safe to dispose.
/// </remarks>
public static class GenAiActivities
{
    /// <summary>
    /// Start an LLM call span. Stamps system + operation + request-model + (optional) max-tokens
    /// + temperature + top-p + top-k. The caller updates the activity post-call with response
    /// model id, finish reasons, and usage tokens via <see cref="StampLlmResponse"/>.
    /// </summary>
    public static Activity? StartLlmCall(
        string system,
        string operation,
        string requestModel,
        int? maxTokens = null,
        double? temperature = null,
        double? topP = null,
        int? topK = null,
        string? agentId = null,
        string? taskId = null)
    {
        var activity = GenAiActivitySource.ActivitySource.StartActivity(
            $"gen_ai.{operation} {requestModel}",
            ActivityKind.Client);

        if (activity is null) return null;

        activity.SetTag(GenAiAttributeNames.System, system);
        activity.SetTag(GenAiAttributeNames.OperationName, operation);
        activity.SetTag(GenAiAttributeNames.RequestModel, requestModel);
        activity.SetTag(GenAiAttributeNames.Framework, "dotnetagents");
        activity.SetTag(GenAiAttributeNames.FrameworkVersion, GenAiActivitySource.FrameworkVersion);

        if (maxTokens is not null) activity.SetTag(GenAiAttributeNames.RequestMaxTokens, maxTokens.Value);
        if (temperature is not null) activity.SetTag(GenAiAttributeNames.RequestTemperature, temperature.Value);
        if (topP is not null) activity.SetTag(GenAiAttributeNames.RequestTopP, topP.Value);
        if (topK is not null) activity.SetTag(GenAiAttributeNames.RequestTopK, topK.Value);

        if (!string.IsNullOrEmpty(agentId)) activity.SetTag(GenAiAttributeNames.AgentId, agentId);
        if (!string.IsNullOrEmpty(taskId)) activity.SetTag(GenAiAttributeNames.TaskId, taskId);

        return activity;
    }

    /// <summary>
    /// Stamp post-call attributes onto an active LLM-call activity. Idempotent — calling
    /// twice overwrites; Activity tag semantics support multi-set.
    /// </summary>
    public static void StampLlmResponse(
        Activity? activity,
        string? responseModel = null,
        string? responseId = null,
        IEnumerable<string>? finishReasons = null,
        long? inputTokens = null,
        long? outputTokens = null,
        decimal? actualCostUsd = null)
    {
        if (activity is null) return;

        if (!string.IsNullOrEmpty(responseModel)) activity.SetTag(GenAiAttributeNames.ResponseModel, responseModel);
        if (!string.IsNullOrEmpty(responseId)) activity.SetTag(GenAiAttributeNames.ResponseId, responseId);
        if (finishReasons is not null)
        {
            activity.SetTag(GenAiAttributeNames.ResponseFinishReasons, string.Join(",", finishReasons));
        }
        if (inputTokens is not null) activity.SetTag(GenAiAttributeNames.UsageInputTokens, inputTokens.Value);
        if (outputTokens is not null) activity.SetTag(GenAiAttributeNames.UsageOutputTokens, outputTokens.Value);
        if (actualCostUsd is not null) activity.SetTag(GenAiAttributeNames.CostActualUsd, (double)actualCostUsd.Value);
    }

    /// <summary>
    /// Start an agent-task span — the outer span wrapping a unit of agent work. Inside this span,
    /// LLM-call and tool-use spans nest naturally so backends can correlate them.
    /// </summary>
    public static Activity? StartAgentTask(
        string agentId,
        string taskId,
        string? agentName = null,
        string? taskName = null,
        string? agentDescription = null)
    {
        var activity = GenAiActivitySource.ActivitySource.StartActivity(
            $"gen_ai.agent.task {agentName ?? agentId}",
            ActivityKind.Internal);

        if (activity is null) return null;

        activity.SetTag(GenAiAttributeNames.OperationName, GenAiOperationNames.AgentTask);
        activity.SetTag(GenAiAttributeNames.AgentId, agentId);
        activity.SetTag(GenAiAttributeNames.TaskId, taskId);
        activity.SetTag(GenAiAttributeNames.Framework, "dotnetagents");
        activity.SetTag(GenAiAttributeNames.FrameworkVersion, GenAiActivitySource.FrameworkVersion);

        if (!string.IsNullOrEmpty(agentName)) activity.SetTag(GenAiAttributeNames.AgentName, agentName);
        if (!string.IsNullOrEmpty(taskName)) activity.SetTag(GenAiAttributeNames.TaskName, taskName);
        if (!string.IsNullOrEmpty(agentDescription)) activity.SetTag(GenAiAttributeNames.AgentDescription, agentDescription);

        return activity;
    }

    /// <summary>
    /// Start a tool-use span. Nests inside an agent-task or routing-decision parent.
    /// </summary>
    public static Activity? StartToolUse(
        string toolName,
        string? toolDescription = null,
        string? toolCallId = null,
        string toolType = "function",
        string? agentId = null,
        string? taskId = null)
    {
        var activity = GenAiActivitySource.ActivitySource.StartActivity(
            $"gen_ai.execute_tool {toolName}",
            ActivityKind.Internal);

        if (activity is null) return null;

        activity.SetTag(GenAiAttributeNames.OperationName, GenAiOperationNames.ExecuteTool);
        activity.SetTag(GenAiAttributeNames.ToolName, toolName);
        activity.SetTag(GenAiAttributeNames.ToolType, toolType);
        activity.SetTag(GenAiAttributeNames.Framework, "dotnetagents");
        activity.SetTag(GenAiAttributeNames.FrameworkVersion, GenAiActivitySource.FrameworkVersion);

        if (!string.IsNullOrEmpty(toolDescription)) activity.SetTag(GenAiAttributeNames.ToolDescription, toolDescription);
        if (!string.IsNullOrEmpty(toolCallId)) activity.SetTag(GenAiAttributeNames.ToolCallId, toolCallId);
        if (!string.IsNullOrEmpty(agentId)) activity.SetTag(GenAiAttributeNames.AgentId, agentId);
        if (!string.IsNullOrEmpty(taskId)) activity.SetTag(GenAiAttributeNames.TaskId, taskId);

        return activity;
    }

    /// <summary>
    /// Start a routing-decision span — DotNetAgents/JARVIS extension. Stamps the
    /// route-decision-id used to correlate the chosen route to its downstream LLM call(s)
    /// and outcomes.
    /// </summary>
    public static Activity? StartRoutingDecision(
        string routeDecisionId,
        string? agentId = null,
        string? taskId = null)
    {
        var activity = GenAiActivitySource.ActivitySource.StartActivity(
            "gen_ai.routing.decision",
            ActivityKind.Internal);

        if (activity is null) return null;

        activity.SetTag(GenAiAttributeNames.OperationName, GenAiOperationNames.RoutingDecision);
        activity.SetTag(GenAiAttributeNames.RouteDecisionId, routeDecisionId);
        activity.SetTag(GenAiAttributeNames.Framework, "dotnetagents");
        activity.SetTag(GenAiAttributeNames.FrameworkVersion, GenAiActivitySource.FrameworkVersion);

        if (!string.IsNullOrEmpty(agentId)) activity.SetTag(GenAiAttributeNames.AgentId, agentId);
        if (!string.IsNullOrEmpty(taskId)) activity.SetTag(GenAiAttributeNames.TaskId, taskId);

        return activity;
    }

    /// <summary>Stamp the chosen tier + (optional) escalation reason onto a routing-decision activity.</summary>
    public static void StampRoutingOutcome(
        Activity? activity,
        string tier,
        string? escalationReason = null,
        decimal? estimatedCostUsd = null)
    {
        if (activity is null) return;
        activity.SetTag(GenAiAttributeNames.RoutingTier, tier);
        if (!string.IsNullOrEmpty(escalationReason))
        {
            activity.SetTag(GenAiAttributeNames.RoutingEscalationReason, escalationReason);
        }
        if (estimatedCostUsd is not null)
        {
            activity.SetTag(GenAiAttributeNames.CostEstimateUsd, (double)estimatedCostUsd.Value);
        }
    }
}
