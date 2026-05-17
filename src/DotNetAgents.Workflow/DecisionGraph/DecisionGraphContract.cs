using System.Text.Json.Serialization;

namespace DotNetAgents.Workflow.DecisionGraph;

/// <summary>
/// JARVIS decision graph v1 contract types. Story 55db0c7d. Mirror of
/// docs/schemas/jarvis-decision-graph.v1.json. The graph is operator-curated
/// data that compiles into existing DotNetAgents StateGraph / behavior-tree /
/// workflow runtimes via <see cref="IDecisionGraphCompiler"/>.
/// </summary>
public sealed class DecisionGraphDefinition
{
    public const string CurrentSchemaVersion = "dna.jarvis.decision-graph.v1";

    [JsonPropertyName("schemaVersion")] public string SchemaVersion { get; set; } = CurrentSchemaVersion;
    [JsonPropertyName("graphKey")] public string GraphKey { get; set; } = string.Empty;
    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
    [JsonPropertyName("status")] public DecisionGraphStatus Status { get; set; } = DecisionGraphStatus.Draft;
    [JsonPropertyName("entryNodeId")] public string EntryNodeId { get; set; } = string.Empty;
    [JsonPropertyName("exitNodeIds")] public List<string> ExitNodeIds { get; set; } = new();
    [JsonPropertyName("toolAllowlist")] public ToolAllowlist ToolAllowlist { get; set; } = new();
    [JsonPropertyName("modelPolicy")] public ModelPolicy ModelPolicy { get; set; } = new();
    [JsonPropertyName("safetyPolicy")] public SafetyPolicy SafetyPolicy { get; set; } = new();
    [JsonPropertyName("variables")] public List<GraphVariable> Variables { get; set; } = new();
    [JsonPropertyName("nodes")] public List<DecisionGraphNode> Nodes { get; set; } = new();
    [JsonPropertyName("edges")] public List<DecisionGraphEdge> Edges { get; set; } = new();
    [JsonPropertyName("geneticMetadata")] public GeneticMetadata? GeneticMetadata { get; set; }
}

public enum DecisionGraphStatus { Draft, Validated, Active, Retired, Rejected }

public sealed class ToolAllowlist
{
    [JsonPropertyName("mcpServices")] public List<string> McpServices { get; set; } = new();
    [JsonPropertyName("tools")] public List<string> Tools { get; set; } = new();
    [JsonPropertyName("memoryScopes")] public List<string> MemoryScopes { get; set; } = new();
    [JsonPropertyName("llmTaskFamilies")] public List<string> LlmTaskFamilies { get; set; } = new();
}

public sealed class ModelPolicy
{
    [JsonPropertyName("strategy")] public ModelStrategy Strategy { get; set; } = ModelStrategy.LocalFirst;
    [JsonPropertyName("requiredCapabilities")] public List<string> RequiredCapabilities { get; set; } = new();
    [JsonPropertyName("allowCommercialFallback")] public bool AllowCommercialFallback { get; set; }
    [JsonPropertyName("commercialFallbackRequiresReason")] public bool CommercialFallbackRequiresReason { get; set; }
    [JsonPropertyName("maxInputTokens")] public int? MaxInputTokens { get; set; }
    [JsonPropertyName("maxOutputTokens")] public int? MaxOutputTokens { get; set; }
}

public enum ModelStrategy
{
    [JsonStringEnumMemberName("local_first")] LocalFirst,
    [JsonStringEnumMemberName("commercial_first")] CommercialFirst,
    [JsonStringEnumMemberName("local_only")] LocalOnly,
    [JsonStringEnumMemberName("commercial_only")] CommercialOnly,
}

public sealed class SafetyPolicy
{
    [JsonPropertyName("requiresConfirmationForMutation")] public bool RequiresConfirmationForMutation { get; set; }
    [JsonPropertyName("maxLoopIterations")] public int MaxLoopIterations { get; set; } = 10;
    [JsonPropertyName("timeoutMs")] public int TimeoutMs { get; set; } = 30000;
    [JsonPropertyName("dataClassification")] public DataClassification DataClassification { get; set; } = DataClassification.OperatorInternal;
}

public enum DataClassification
{
    [JsonStringEnumMemberName("public")] Public,
    [JsonStringEnumMemberName("operator_internal")] OperatorInternal,
    [JsonStringEnumMemberName("user_personal")] UserPersonal,
    [JsonStringEnumMemberName("secret_referenced")] SecretReferenced,
}

public sealed class GraphVariable
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("type")] public string Type { get; set; } = "string";
    [JsonPropertyName("source")] public string? Source { get; set; }
}

public sealed class DecisionGraphNode
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("type")] public DecisionGraphNodeType Type { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("config")] public Dictionary<string, object?> Config { get; set; } = new();
    [JsonPropertyName("writes")] public List<string> Writes { get; set; } = new();
    [JsonPropertyName("isMutation")] public bool IsMutation { get; set; }
}

public enum DecisionGraphNodeType
{
    [JsonStringEnumMemberName("intent.classify")] IntentClassify,
    [JsonStringEnumMemberName("memory.retrieve")] MemoryRetrieve,
    [JsonStringEnumMemberName("tool.select")] ToolSelect,
    [JsonStringEnumMemberName("tool.call")] ToolCall,
    [JsonStringEnumMemberName("policy.gate")] PolicyGate,
    [JsonStringEnumMemberName("llm.reason")] LlmReason,
    [JsonStringEnumMemberName("response.compose")] ResponseCompose,
    [JsonStringEnumMemberName("state.transition")] StateTransition,
    [JsonStringEnumMemberName("quality.score")] QualityScore,
    [JsonStringEnumMemberName("human.confirm")] HumanConfirm,
    [JsonStringEnumMemberName("subgraph.invoke")] SubgraphInvoke,
}

public sealed class DecisionGraphEdge
{
    [JsonPropertyName("from")] public string From { get; set; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; set; } = string.Empty;
    [JsonPropertyName("condition")] public EdgeCondition Condition { get; set; } = new();
    [JsonPropertyName("loopBound")] public int? LoopBound { get; set; }
}

public sealed class EdgeCondition
{
    [JsonPropertyName("type")] public EdgeConditionType Type { get; set; } = EdgeConditionType.Always;

    /// <summary>Free-form payload for condition-specific fields (path, value, domains, threshold, etc.).</summary>
    [JsonExtensionData] public Dictionary<string, System.Text.Json.JsonElement>? Extra { get; set; }
}

public enum EdgeConditionType
{
    [JsonStringEnumMemberName("always")] Always,
    [JsonStringEnumMemberName("state.equals")] StateEquals,
    [JsonStringEnumMemberName("state.exists")] StateExists,
    [JsonStringEnumMemberName("score.atLeast")] ScoreAtLeast,
    [JsonStringEnumMemberName("intent.matches")] IntentMatches,
    [JsonStringEnumMemberName("tool.succeeded")] ToolSucceeded,
    [JsonStringEnumMemberName("tool.failed")] ToolFailed,
    [JsonStringEnumMemberName("policy.allowed")] PolicyAllowed,
    [JsonStringEnumMemberName("policy.denied")] PolicyDenied,
    [JsonStringEnumMemberName("needs.confirmation")] NeedsConfirmation,
    [JsonStringEnumMemberName("llm.schemaField.equals")] LlmSchemaFieldEquals,
}

public sealed class GeneticMetadata
{
    [JsonPropertyName("genomeTags")] public List<string> GenomeTags { get; set; } = new();
    [JsonPropertyName("parentGraphVersions")] public List<string> ParentGraphVersions { get; set; } = new();
    [JsonPropertyName("mutationNotes")] public string? MutationNotes { get; set; }
    [JsonPropertyName("fitnessTaskFamilies")] public List<string> FitnessTaskFamilies { get; set; } = new();
}
