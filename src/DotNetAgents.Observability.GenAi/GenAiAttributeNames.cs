// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Observability.GenAi;

/// <summary>
/// Canonical attribute names per the OpenTelemetry GenAI semantic conventions
/// (opentelemetry.io/docs/specs/semconv/gen-ai/). Implementations MUST use these
/// exact names — backends like Datadog, Langfuse, Braintrust render based on
/// the spec attribute keys and will not detect custom variants.
/// </summary>
/// <remarks>
/// Story 40a4ebf1 (Phase 2B). Reference subset of the ratified GenAI semconv as of
/// the v1 stable release. Additional attributes (gen_ai.choice.*, gen_ai.tool.* call args,
/// gen_ai.embeddings.*, etc.) can be added as DotNetAgents needs them; the current set
/// covers the bulk of agent/LLM/tool/framework spans.
/// </remarks>
public static class GenAiAttributeNames
{
    // ---- Operation ----

    /// <summary>The provider/system performing the GenAI call. Examples: <c>anthropic</c>, <c>openai</c>, <c>vllm</c>, <c>ollama</c>, <c>local</c>, <c>azure-openai</c>.</summary>
    public const string System = "gen_ai.system";

    /// <summary>What kind of operation the span represents. Examples: <c>chat</c>, <c>text_completion</c>, <c>embeddings</c>, <c>classify</c>, <c>execute_tool</c>.</summary>
    public const string OperationName = "gen_ai.operation.name";

    // ---- Request ----

    /// <summary>Model id requested by the caller. Examples: <c>gpt-4o-2024-08-06</c>, <c>claude-3-5-sonnet-20240620</c>, <c>qwen2.5-coder:32b</c>.</summary>
    public const string RequestModel = "gen_ai.request.model";

    /// <summary>Max tokens cap supplied in the request, when present.</summary>
    public const string RequestMaxTokens = "gen_ai.request.max_tokens";

    /// <summary>Temperature supplied in the request, when present.</summary>
    public const string RequestTemperature = "gen_ai.request.temperature";

    /// <summary>Top-P supplied in the request, when present.</summary>
    public const string RequestTopP = "gen_ai.request.top_p";

    /// <summary>Top-K supplied in the request, when present.</summary>
    public const string RequestTopK = "gen_ai.request.top_k";

    // ---- Response ----

    /// <summary>Model id reported by the provider in the response. May differ from the requested model when the provider routes/aliases.</summary>
    public const string ResponseModel = "gen_ai.response.model";

    /// <summary>Provider-supplied response identifier (e.g. OpenAI completion id).</summary>
    public const string ResponseId = "gen_ai.response.id";

    /// <summary>Reasons the response stopped, comma-separated. Examples: <c>stop</c>, <c>length</c>, <c>tool_calls</c>, <c>content_filter</c>.</summary>
    public const string ResponseFinishReasons = "gen_ai.response.finish_reasons";

    // ---- Usage ----

    /// <summary>Input (prompt) token count for the call.</summary>
    public const string UsageInputTokens = "gen_ai.usage.input_tokens";

    /// <summary>Output (completion) token count for the call.</summary>
    public const string UsageOutputTokens = "gen_ai.usage.output_tokens";

    // ---- Agent ----

    /// <summary>Stable identifier for the agent emitting this span.</summary>
    public const string AgentId = "gen_ai.agent.id";

    /// <summary>Execution role the emitting agent was serving when it made the model call.</summary>
    public const string AgentRole = "dna.agent.role";

    /// <summary>Operator-readable agent name.</summary>
    public const string AgentName = "gen_ai.agent.name";

    /// <summary>Agent description for retrieval/dashboards.</summary>
    public const string AgentDescription = "gen_ai.agent.description";

    // ---- Task ----

    /// <summary>Identifier for the task this span is part of (typically a story id or workflow run id).</summary>
    public const string TaskId = "gen_ai.task.id";

    /// <summary>Stable execution domain or task family, such as <c>intent-classification</c> or <c>sdlc.grooming</c>.</summary>
    public const string TaskDomain = "dna.task.domain";

    /// <summary>Operator-readable task name.</summary>
    public const string TaskName = "gen_ai.task.name";

    // ---- Tool ----

    /// <summary>Name of the tool being invoked.</summary>
    public const string ToolName = "gen_ai.tool.name";

    /// <summary>Tool description.</summary>
    public const string ToolDescription = "gen_ai.tool.description";

    /// <summary>Tool call identifier (for matching tool-use to tool-result events).</summary>
    public const string ToolCallId = "gen_ai.tool.call.id";

    /// <summary>Type of tool — <c>function</c>, <c>retrieval</c>, <c>code_interpreter</c>, etc.</summary>
    public const string ToolType = "gen_ai.tool.type";

    // ---- Framework span attributes (DotNetAgents-specific extensions) ----

    /// <summary>Framework that produced the span — for DotNetAgents, always <c>dotnetagents</c>.</summary>
    public const string Framework = "gen_ai.framework";

    /// <summary>Framework version string.</summary>
    public const string FrameworkVersion = "gen_ai.framework.version";

    /// <summary>Stable id correlating a routing decision to its downstream LLM call(s) and outcomes. Used by JARVIS's IModelFitnessPolicy chain.</summary>
    public const string RouteDecisionId = "gen_ai.routing.decision_id";

    /// <summary>Cognition tier the routing decision selected: <c>workhorse</c>, <c>medium</c>, <c>crow</c>, <c>external</c>.</summary>
    public const string RoutingTier = "gen_ai.routing.tier";

    /// <summary>Why a routing decision escalated to external — when applicable.</summary>
    public const string RoutingEscalationReason = "gen_ai.routing.escalation_reason";

    /// <summary>Stable gateway or broker id that actually served or would have served the model call.</summary>
    public const string GatewayId = "dna.gateway.id";

    /// <summary>Warm/cold/unknown snapshot for the selected local model slot.</summary>
    public const string GatewayWarmState = "dna.gateway.warm_state";

    /// <summary>Whether the local model was warm, loaded on demand, failed to load, or was not applicable.</summary>
    public const string GatewayLoadState = "dna.gateway.load_state";

    /// <summary>Execution-path explanation for choosing local vs external.</summary>
    public const string LocalExternalReason = "dna.routing.local_external_reason";

    /// <summary>Compact representation of the fallback chain the runtime attempted.</summary>
    public const string FallbackChain = "dna.routing.fallback_chain";

    /// <summary>Prompt artifact identifier, without embedding the raw prompt text.</summary>
    public const string PromptArtifactId = "dna.prompt.artifact_id";

    /// <summary>Prompt artifact version or semantic revision label.</summary>
    public const string PromptArtifactVersion = "dna.prompt.artifact_version";

    /// <summary>Model tier such as workhorse, medium, mini, frontier, or unknown.</summary>
    public const string ModelTier = "dna.model.tier";

    /// <summary>Model size family such as 7b, 32b, 70b, or unknown.</summary>
    public const string ModelSize = "dna.model.size";

    /// <summary>Cache hit/miss/not-applicable state for the invocation.</summary>
    public const string CacheStatus = "dna.cache.status";

    /// <summary>Estimated cost in USD for this operation (pre-call).</summary>
    public const string CostEstimateUsd = "gen_ai.cost.estimate_usd";

    /// <summary>Actual cost in USD for this operation (post-call).</summary>
    public const string CostActualUsd = "gen_ai.cost.actual_usd";

    /// <summary>Correlation story id when a call is attributable to a specific story.</summary>
    public const string CorrelationStoryId = "dna.correlation.story_id";

    /// <summary>Correlation run id when a call belongs to a workflow run.</summary>
    public const string CorrelationRunId = "dna.correlation.run_id";

    /// <summary>Correlation command id when a call belongs to a spoken or operator command.</summary>
    public const string CorrelationCommandId = "dna.correlation.command_id";

    /// <summary>Correlation user id when a call is attributable to a known user.</summary>
    public const string CorrelationUserId = "dna.correlation.user_id";

    /// <summary>Correlation action id when a model decision maps to a downstream action.</summary>
    public const string CorrelationActionId = "dna.correlation.action_id";

    /// <summary>Correlation artifact id when a model decision produced or updated a concrete artifact.</summary>
    public const string CorrelationArtifactId = "dna.correlation.artifact_id";
}

/// <summary>
/// Canonical operation-name values for <see cref="GenAiAttributeNames.OperationName"/>.
/// </summary>
public static class GenAiOperationNames
{
    /// <summary>Multi-turn chat completion.</summary>
    public const string Chat = "chat";

    /// <summary>Single-prompt text completion.</summary>
    public const string TextCompletion = "text_completion";

    /// <summary>Embeddings generation.</summary>
    public const string Embeddings = "embeddings";

    /// <summary>Classification operation.</summary>
    public const string Classify = "classify";

    /// <summary>Tool invocation.</summary>
    public const string ExecuteTool = "execute_tool";

    /// <summary>Routing decision span (DotNetAgents/JARVIS extension).</summary>
    public const string RoutingDecision = "routing.decision";

    /// <summary>Agent task — wraps a unit of agent work.</summary>
    public const string AgentTask = "agent.task";
}

/// <summary>
/// Canonical system-name values for <see cref="GenAiAttributeNames.System"/>.
/// </summary>
public static class GenAiSystemNames
{
    public const string Anthropic = "anthropic";
    public const string OpenAi = "openai";
    public const string AzureOpenAi = "azure-openai";
    public const string Google = "google";
    public const string AwsBedrock = "aws.bedrock";
    public const string Vllm = "vllm";
    public const string Ollama = "ollama";
    public const string Mlx = "mlx";
    public const string LmStudio = "lm-studio";
    public const string Cohere = "cohere";
    public const string Groq = "groq";
    public const string Mistral = "mistral";
    public const string Together = "together";
    public const string Local = "local";
}
