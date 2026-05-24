// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace DotNetAgents.Runtime;

public sealed class InProcessDelegationBroker : IDelegationBroker
{
    private readonly IAgentRuntime _runtime;
    private readonly IDelegationPolicy _policy;
    private readonly IDelegatedRunStore _store;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeRuns = new(StringComparer.Ordinal);

    public InProcessDelegationBroker(
        IAgentRuntime runtime,
        IDelegationPolicy policy,
        IDelegatedRunStore store)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<DelegatedAgentRunResult> StartAsync(
        DelegatedAgentRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var created = await _store.CreateAsync(new DelegatedAgentRun
        {
            ParentSessionId = request.ParentSessionId,
            ParentActorId = request.ParentActorId,
            ChildActorId = request.ChildActorId,
            Task = request.Task,
            AllowedTools = request.AllowedTools,
            DeniedTools = request.DeniedTools,
            BudgetTokens = request.BudgetTokens,
            Timeout = request.Timeout,
            CurrentDepth = request.CurrentDepth,
            MaxDepth = request.MaxDepth,
            ArtifactRefs = request.ArtifactRefs,
            Metadata = request.Metadata
        }, cancellationToken).ConfigureAwait(false);

        var decision = _policy.Evaluate(request);
        if (!decision.Allowed)
        {
            var failed = await CompleteAsync(created with
            {
                Status = DelegatedAgentRunStatus.Failed,
                ErrorMessage = decision.Reason
            }, cancellationToken).ConfigureAwait(false);
            return new DelegatedAgentRunResult(failed, decision.Reason ?? "Delegated run rejected by policy.", failed.ArtifactRefs, failed.TrajectoryId);
        }

        var running = await _store.UpdateAsync(created with
        {
            Status = DelegatedAgentRunStatus.Running,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        }, cancellationToken).ConfigureAwait(false);

        using var timeoutCts = new CancellationTokenSource(request.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        if (!_activeRuns.TryAdd(running.Id, linkedCts))
            throw new InvalidOperationException($"Delegated run '{running.Id}' is already active.");

        try
        {
            var runtimeResult = await _runtime.RunAsync(new AgentRunRequest
            {
                ActorId = request.ChildActorId,
                UserInput = request.Task,
                RunMode = AgentRunMode.Delegated,
                ParentSessionId = request.ParentSessionId,
                DelegatedFromActorId = request.ParentActorId,
                ModelRoute = request.ModelRoute,
                Metadata = BuildRuntimeMetadata(request)
            }, linkedCts.Token).ConfigureAwait(false);

            var completedStatus = runtimeResult.Status is AgentSessionStatus.Completed or AgentSessionStatus.CompletedWithToolErrors
                ? DelegatedAgentRunStatus.Completed
                : DelegatedAgentRunStatus.Failed;
            var completed = await CompleteAsync(running with
            {
                ChildSessionId = runtimeResult.Session.Id,
                Status = completedStatus,
                ResultSummary = runtimeResult.AssistantMessage,
                TrajectoryId = runtimeResult.Trajectory.Id,
                ArtifactRefs = runtimeResult.Trajectory.ArtifactRefs
            }, CancellationToken.None).ConfigureAwait(false);

            return new DelegatedAgentRunResult(
                completed,
                runtimeResult.AssistantMessage,
                runtimeResult.Trajectory.ArtifactRefs,
                runtimeResult.Trajectory.Id);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            var timedOut = await CompleteAsync(running with
            {
                Status = DelegatedAgentRunStatus.TimedOut,
                ErrorMessage = "Delegated run timed out."
            }, CancellationToken.None).ConfigureAwait(false);
            return new DelegatedAgentRunResult(timedOut, timedOut.ErrorMessage ?? "Delegated run timed out.", timedOut.ArtifactRefs, timedOut.TrajectoryId);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            var cancelled = await CompleteAsync(running with
            {
                Status = DelegatedAgentRunStatus.Cancelled,
                ErrorMessage = "Delegated run cancelled."
            }, CancellationToken.None).ConfigureAwait(false);
            return new DelegatedAgentRunResult(cancelled, cancelled.ErrorMessage ?? "Delegated run cancelled.", cancelled.ArtifactRefs, cancelled.TrajectoryId);
        }
        finally
        {
            _activeRuns.TryRemove(running.Id, out _);
        }
    }

    public Task<DelegatedAgentRun?> GetStatusAsync(
        string runId,
        CancellationToken cancellationToken = default) =>
        _store.GetAsync(runId, cancellationToken);

    public async Task<bool> CancelAsync(
        string runId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (_activeRuns.TryGetValue(runId, out var cts))
        {
            await cts.CancelAsync().ConfigureAwait(false);
            return true;
        }

        var run = await _store.GetAsync(runId, cancellationToken).ConfigureAwait(false);
        if (run is null || run.Status is DelegatedAgentRunStatus.Completed or DelegatedAgentRunStatus.Failed or DelegatedAgentRunStatus.Cancelled or DelegatedAgentRunStatus.TimedOut)
            return false;

        await CompleteAsync(run with
        {
            Status = DelegatedAgentRunStatus.Cancelled,
            ErrorMessage = string.IsNullOrWhiteSpace(reason) ? "Delegated run cancelled." : reason
        }, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private Task<DelegatedAgentRun> CompleteAsync(
        DelegatedAgentRun run,
        CancellationToken cancellationToken) =>
        _store.UpdateAsync(run with
        {
            CompletedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        }, cancellationToken);

    private static IReadOnlyDictionary<string, string> BuildRuntimeMetadata(DelegatedAgentRunRequest request)
    {
        var metadata = new Dictionary<string, string>(request.Metadata, StringComparer.Ordinal)
        {
            ["delegation.parentSessionId"] = request.ParentSessionId,
            ["delegation.parentActorId"] = request.ParentActorId,
            ["delegation.maxDepth"] = request.MaxDepth.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["delegation.currentDepth"] = request.CurrentDepth.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["delegation.allowedTools"] = string.Join(",", request.AllowedTools.Order(StringComparer.OrdinalIgnoreCase)),
            ["delegation.deniedTools"] = string.Join(",", request.DeniedTools.Order(StringComparer.OrdinalIgnoreCase))
        };
        if (request.BudgetTokens is { } budget)
            metadata["delegation.budgetTokens"] = budget.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new ReadOnlyDictionary<string, string>(metadata);
    }
}
