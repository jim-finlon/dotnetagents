// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.BehaviorTrees;

namespace DotNetAgents.Agents.ControlLoops;

/// <summary>
/// Behavior-tree helper nodes that consult <see cref="IControlLoopGovernanceHook"/>
/// before triggering work through <see cref="IWorkflowTriggerSink"/> /
/// <see cref="IQueueTriggerSink"/>. Story feef53a3.
///
/// Verdict mapping for the returned BehaviorTreeNodeStatus:
///   - Allow         → run trigger; Success on sink success, Failure on sink failure
///   - Deny          → Failure (don't trigger). Caller surfaces verdict.AttentionForOperator.
///   - Defer         → Failure (caller's parent node should rewrap with Cooldown).
///   - NeedsApproval → Failure (caller routes to approval queue separately).
///
/// All non-Allow verdicts publish a control_loop.attention.total metric so
/// dashboards see governance pressure even when the caller doesn't surface
/// the AttentionItem directly.
/// </summary>
public static class GovernedTriggerHelpers
{
    public static IBehaviorTreeNode<TContext> GovernanceCheckedTriggerWorkflow<TContext>(
        string nodeName,
        IControlLoopGovernanceHook hook,
        Func<TContext, GovernanceCheckRequest> requestBuilder,
        IWorkflowTriggerSink sink,
        Func<TContext, WorkflowTriggerRequest> triggerBuilder,
        ControlLoopMetricsRecorder? metrics = null) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(hook);
        ArgumentNullException.ThrowIfNull(requestBuilder);
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(triggerBuilder);

        return new ActionNode<TContext>(nodeName, async (ctx, ct) =>
        {
            var checkRequest = requestBuilder(ctx);
            var verdict = await hook.EvaluateAsync(checkRequest, ct).ConfigureAwait(false);
            if (verdict.Decision != GovernanceDecision.Allow)
            {
                EmitAttentionMetric(metrics, verdict, checkRequest);
                return BehaviorTreeNodeStatus.Failure;
            }
            try
            {
                var trigger = triggerBuilder(ctx);
                var runId = await sink.TriggerAsync(trigger, ct).ConfigureAwait(false);
                return runId is null ? BehaviorTreeNodeStatus.Failure : BehaviorTreeNodeStatus.Success;
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                return BehaviorTreeNodeStatus.Failure;
            }
        });
    }

    public static IBehaviorTreeNode<TContext> GovernanceCheckedEnqueueWork<TContext>(
        string nodeName,
        IControlLoopGovernanceHook hook,
        Func<TContext, GovernanceCheckRequest> requestBuilder,
        IQueueTriggerSink sink,
        Func<TContext, QueueTriggerRequest> queueBuilder,
        ControlLoopMetricsRecorder? metrics = null) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(hook);
        ArgumentNullException.ThrowIfNull(requestBuilder);
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(queueBuilder);

        return new ActionNode<TContext>(nodeName, async (ctx, ct) =>
        {
            var checkRequest = requestBuilder(ctx);
            var verdict = await hook.EvaluateAsync(checkRequest, ct).ConfigureAwait(false);
            if (verdict.Decision != GovernanceDecision.Allow)
            {
                EmitAttentionMetric(metrics, verdict, checkRequest);
                return BehaviorTreeNodeStatus.Failure;
            }
            try
            {
                var enqueue = queueBuilder(ctx);
                var msgId = await sink.EnqueueAsync(enqueue, ct).ConfigureAwait(false);
                return msgId is null ? BehaviorTreeNodeStatus.Failure : BehaviorTreeNodeStatus.Success;
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                return BehaviorTreeNodeStatus.Failure;
            }
        });
    }

    private static void EmitAttentionMetric(ControlLoopMetricsRecorder? metrics, GovernanceVerdict verdict, GovernanceCheckRequest request)
    {
        if (metrics is null) return;
        var severity = verdict.Decision switch
        {
            GovernanceDecision.Deny => "P1",
            GovernanceDecision.NeedsApproval => "P2",
            GovernanceDecision.Defer => "P3",
            _ => "P3",
        };
        var attention = new AttentionItem(
            Code: $"governance.{verdict.ReasonCode}",
            Title: verdict.ReasonMessage,
            Severity: severity,
            DetectedAtUtc: DateTime.UtcNow,
            Detail: $"action={request.ActionKind} blast_radius={request.BlastRadius} actor={request.ActorId}");
        metrics.Attention(attention, request.CurrentPosture is not null ? "GovernanceGated" : "Unknown");
    }
}
