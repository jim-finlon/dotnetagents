// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Tools;

namespace DotNetAgents.Runtime;

public interface IAgentRuntime
{
    Task<AgentRunResult> RunAsync(
        AgentRunRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAgentSessionStore
{
    Task<AgentSession> CreateSessionAsync(
        AgentSession session,
        CancellationToken cancellationToken = default);

    Task<AgentSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<AgentSession> UpdateSessionStatusAsync(
        string sessionId,
        AgentSessionStatus status,
        CancellationToken cancellationToken = default);

    Task<AgentMessage> AppendMessageAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default);

    Task<ToolInvocation> AppendToolInvocationAsync(
        ToolInvocation invocation,
        CancellationToken cancellationToken = default);

    Task<ProviderCall> AppendProviderCallAsync(
        ProviderCall providerCall,
        CancellationToken cancellationToken = default);

    Task<ContextSnapshot> AppendContextSnapshotAsync(
        ContextSnapshot snapshot,
        CancellationToken cancellationToken = default);

    Task<AgentSessionActivity> ReadActivityAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}

public interface ITrajectoryRecorder
{
    Task<TrajectoryArtifact> RecordAsync(
        AgentSessionActivity activity,
        ModelRouteMetadata modelRoute,
        CancellationToken cancellationToken = default);
}

public interface IAgentTurnModel
{
    Task<AgentModelResponse> GenerateAsync(
        AgentModelRequest request,
        CancellationToken cancellationToken = default);
}

public interface IToolsetResolver
{
    Task<ITool?> ResolveAsync(
        string toolName,
        CancellationToken cancellationToken = default);
}

public interface IPromptContextAssembler
{
    // Future seam for memory compaction, gateway session replay, EvaluationSandbox fixtures, and prompt-library variants.
}

public interface IScheduledAgentRunScheduler
{
    // Future seam for cron-like jobs and durable scheduled run wakeups.
}

public interface IDelegatedSessionRouter
{
    // Future seam for child/delegated sessions that cross agent, gateway, or operator-console boundaries.
}

public interface IDelegationBroker
{
    Task<DelegatedAgentRunResult> StartAsync(
        DelegatedAgentRunRequest request,
        CancellationToken cancellationToken = default);

    Task<DelegatedAgentRun?> GetStatusAsync(
        string runId,
        CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(
        string runId,
        string reason,
        CancellationToken cancellationToken = default);
}

public interface IDelegationPolicy
{
    DelegationPolicyDecision Evaluate(DelegatedAgentRunRequest request);
}

public interface IDelegatedRunStore
{
    Task<DelegatedAgentRun> CreateAsync(
        DelegatedAgentRun run,
        CancellationToken cancellationToken = default);

    Task<DelegatedAgentRun?> GetAsync(
        string runId,
        CancellationToken cancellationToken = default);

    Task<DelegatedAgentRun> UpdateAsync(
        DelegatedAgentRun run,
        CancellationToken cancellationToken = default);
}

public interface IExecutionLease
{
    string LeaseId { get; }
    string ProviderName { get; }
    string ActorId { get; }
    string Purpose { get; }
    string BaseCommit { get; }
    string? BranchName { get; }
    string RootPath { get; }
    DateTimeOffset CreatedAtUtc { get; }
    DateTimeOffset? ExpiresAtUtc { get; }
    IReadOnlySet<string> AllowedOperations { get; }
    ExecutionBlastRadius BlastRadius { get; }
    ExecutionPersistenceMode PersistenceMode { get; }
    ExecutionCredentialMode CredentialMode { get; }
    ExecutionNetworkMode NetworkMode { get; }
    ExecutionCleanupGuarantee CleanupGuarantee { get; }
}

public interface IExecutionEnvironmentProvider
{
    ExecutionEnvironmentProviderMetadata Metadata { get; }

    Task<ExecutionLease> CreateLeaseAsync(
        ExecutionLeaseRequest request,
        CancellationToken cancellationToken = default);

    Task<ExecutionCleanupReceipt> CleanupAsync(
        ExecutionCleanupRequest request,
        CancellationToken cancellationToken = default);
}

public interface ICommandExecutor
{
    Task<CommandExecutionResult> ExecuteAsync(
        CommandExecutionRequest request,
        CancellationToken cancellationToken = default);
}

public interface IArtifactCollector
{
    Task<ArtifactCollectionResult> CollectAsync(
        ArtifactCollectionRequest request,
        CancellationToken cancellationToken = default);
}

public interface IEnvironmentCleanupPolicy
{
    CleanupPolicyDecision Evaluate(
        ExecutionLease lease,
        string requestedPath);
}
