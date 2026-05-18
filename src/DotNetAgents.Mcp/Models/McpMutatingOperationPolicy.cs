namespace DotNetAgents.Mcp.Models;

/// <summary>
/// Standard MCP tool mutation classifications for policy wrappers.
/// </summary>
public static class McpMutationKinds
{
    /// <summary>The tool only reads or describes state.</summary>
    public const string ReadOnly = "read_only";

    /// <summary>The tool can create, update, delete, publish, deploy, claim, close, or otherwise mutate state.</summary>
    public const string Mutating = "mutating";
}

/// <summary>
/// Standard error codes emitted by mutating-operation policy wrappers.
/// </summary>
public static class McpMutatingOperationPolicyErrorCodes
{
    /// <summary>A mutating tool was refused because required workflow context was missing.</summary>
    public const string MissingPreconditions = "MCP_MUTATING_OPERATION_PRECONDITIONS_MISSING";
}

/// <summary>
/// Caller and workflow context supplied to a mutating-operation policy wrapper.
/// </summary>
public sealed record McpMutatingOperationContext
{
    /// <summary>Durable actor id, such as workstation-agent.</summary>
    public string? ActorId { get; init; }

    /// <summary>Actor type, such as WorkstationSession or AgentInstance.</summary>
    public string? ActorType { get; init; }

    /// <summary>Canonical story, event, or task reference authorizing the mutation.</summary>
    public string? StoryId { get; init; }

    /// <summary>Request correlation id propagated through the caller and service logs.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Dedicated worktree path, when a file-system mutation is involved.</summary>
    public string? WorktreePath { get; init; }

    /// <summary>Source-control branch associated with the mutation, when applicable.</summary>
    public string? BranchName { get; init; }

    /// <summary>Whether the service verified the worktree was clean before the mutation.</summary>
    public bool? IsWorktreeClean { get; init; }

    /// <summary>Non-secret approval, receipt, or change-window references required by service policy.</summary>
    public IReadOnlyList<string> ApprovalReferences { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Preconditions a service applies before allowing mutating MCP tools to execute.
/// </summary>
public sealed record McpMutatingOperationPolicy
{
    /// <summary>Require a durable actor id.</summary>
    public bool RequireActorId { get; init; } = true;

    /// <summary>Require an actor type.</summary>
    public bool RequireActorType { get; init; } = true;

    /// <summary>Require a canonical SDLC story, event, or task reference.</summary>
    public bool RequireStoryId { get; init; } = true;

    /// <summary>Require a correlation id for audit and log stitching.</summary>
    public bool RequireCorrelationId { get; init; } = true;

    /// <summary>Require a dedicated worktree path for file-system mutations.</summary>
    public bool RequireWorktreePath { get; init; } = true;

    /// <summary>Require an explicit clean-worktree signal before the mutation.</summary>
    public bool RequireCleanWorktree { get; init; }

    /// <summary>Require at least one non-secret approval or receipt reference.</summary>
    public bool RequireApprovalReference { get; init; }

    /// <summary>Human-readable remediation for this service's adoption of the shared contract.</summary>
    public string RemediationGuidance { get; init; } =
        "Provide actor, story, worktree, and correlation context before retrying the mutating MCP tool.";
}

/// <summary>
/// Policy evaluation result for an MCP tool call.
/// </summary>
public sealed record McpMutatingOperationPolicyDecision
{
    /// <summary>Whether the tool call may proceed.</summary>
    public bool Allowed { get; init; }

    /// <summary>Stable missing-precondition names that blocked the call.</summary>
    public IReadOnlyList<string> MissingPreconditions { get; init; } = Array.Empty<string>();

    /// <summary>Structured MCP response a provider can return directly when refusing the call.</summary>
    public McpToolCallResponse Response { get; init; } = new();
}

/// <summary>
/// Shared evaluator used by MCP services before running process-critical mutating tools.
/// </summary>
public static class McpMutatingOperationPolicyWrapper
{
    /// <summary>
    /// Evaluates the supplied context and returns either an allow decision or a structured refusal response.
    /// Read-only tools are always allowed by this wrapper; service-local auth still applies outside this contract.
    /// </summary>
    public static McpMutatingOperationPolicyDecision Evaluate(
        string serviceName,
        string toolName,
        string mutationKind,
        McpMutatingOperationContext? context,
        McpMutatingOperationPolicy? policy = null)
    {
        policy ??= new McpMutatingOperationPolicy();
        context ??= new McpMutatingOperationContext();

        if (!string.Equals(mutationKind, McpMutationKinds.Mutating, StringComparison.OrdinalIgnoreCase))
        {
            return AllowedDecision(serviceName, toolName, mutationKind, context);
        }

        var missing = FindMissingPreconditions(context, policy);
        if (missing.Count == 0)
        {
            return AllowedDecision(serviceName, toolName, mutationKind, context);
        }

        var metadata = BuildSafeMetadata(serviceName, toolName, mutationKind, context);
        metadata["missingPreconditions"] = string.Join(",", missing);

        return new McpMutatingOperationPolicyDecision
        {
            Allowed = false,
            MissingPreconditions = missing,
            Response = new McpToolCallResponse
            {
                Success = false,
                ErrorCode = McpMutatingOperationPolicyErrorCodes.MissingPreconditions,
                Error = "Mutating MCP tool call refused because required workflow preconditions were missing.",
                Summary = $"Refused mutating MCP tool '{toolName}' on '{serviceName}'.",
                Guidance = policy.RemediationGuidance,
                SuggestedNextSteps =
                [
                    "Attach a canonical story or event reference.",
                    "Attach durable actor and correlation metadata.",
                    "Retry from a dedicated, validated worktree when file-system state can change."
                ],
                Metadata = metadata,
                Remediation = new McpRemediation
                {
                    RemediationKind = "workflow",
                    ServiceName = serviceName,
                    ToolName = toolName,
                    ErrorCode = McpMutatingOperationPolicyErrorCodes.MissingPreconditions,
                    FailedCapability = "mutating_operation_policy",
                    Guidance = policy.RemediationGuidance,
                    SuggestedNextSteps =
                    [
                        "Claim or link the SDLC story before mutating.",
                        "Populate actorId, actorType, storyId, correlationId, and worktreePath.",
                        "Do not bypass service-local authentication or authorization."
                    ],
                    Metadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase)
                }
            }
        };
    }

    private static McpMutatingOperationPolicyDecision AllowedDecision(
        string serviceName,
        string toolName,
        string mutationKind,
        McpMutatingOperationContext context)
    {
        return new McpMutatingOperationPolicyDecision
        {
            Allowed = true,
            Response = new McpToolCallResponse
            {
                Success = true,
                Summary = "MCP mutating-operation policy preconditions satisfied.",
                Metadata = BuildSafeMetadata(serviceName, toolName, mutationKind, context)
            }
        };
    }

    private static List<string> FindMissingPreconditions(
        McpMutatingOperationContext context,
        McpMutatingOperationPolicy policy)
    {
        var missing = new List<string>();

        AddIf(policy.RequireActorId && string.IsNullOrWhiteSpace(context.ActorId), "actorId");
        AddIf(policy.RequireActorType && string.IsNullOrWhiteSpace(context.ActorType), "actorType");
        AddIf(policy.RequireStoryId && string.IsNullOrWhiteSpace(context.StoryId), "storyId");
        AddIf(policy.RequireCorrelationId && string.IsNullOrWhiteSpace(context.CorrelationId), "correlationId");
        AddIf(policy.RequireWorktreePath && string.IsNullOrWhiteSpace(context.WorktreePath), "worktreePath");
        AddIf(policy.RequireCleanWorktree && context.IsWorktreeClean is not true, "cleanWorktree");
        AddIf(policy.RequireApprovalReference && context.ApprovalReferences.Count == 0, "approvalReference");

        return missing;

        void AddIf(bool condition, string name)
        {
            if (condition)
            {
                missing.Add(name);
            }
        }
    }

    private static Dictionary<string, string> BuildSafeMetadata(
        string serviceName,
        string toolName,
        string mutationKind,
        McpMutatingOperationContext context)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["serviceName"] = serviceName,
            ["toolName"] = toolName,
            ["mutationKind"] = mutationKind
        };

        AddIfPresent("actorId", context.ActorId);
        AddIfPresent("actorType", context.ActorType);
        AddIfPresent("storyId", context.StoryId);
        AddIfPresent("correlationId", context.CorrelationId);
        AddIfPresent("branchName", context.BranchName);

        if (context.ApprovalReferences.Count > 0)
        {
            metadata["approvalReferences"] = string.Join(",", context.ApprovalReferences);
        }

        if (context.IsWorktreeClean.HasValue)
        {
            metadata["isWorktreeClean"] = context.IsWorktreeClean.Value.ToString();
        }

        return metadata;

        void AddIfPresent(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                metadata[key] = value;
            }
        }
    }
}
