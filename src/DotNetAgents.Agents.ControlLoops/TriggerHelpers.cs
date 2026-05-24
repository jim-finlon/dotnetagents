// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.BehaviorTrees;

namespace DotNetAgents.Agents.ControlLoops;

/// <summary>
/// Behavior-tree helper nodes that bridge a reactive policy to durable
/// execution via <see cref="IWorkflowTriggerSink"/> / <see cref="IQueueTriggerSink"/>.
/// Story 2d0994f8.
///
/// Trees use these to enqueue or start work *without* embedding the entire
/// business workflow. Trees stay as policy layers; orchestration happens in
/// the workflow runtime / queue dispatcher.
///
/// All helpers stamp the supplied <see cref="TriggerCorrelation"/> onto the
/// outbound request so operators can answer "why was this work started?"
/// without reverse-engineering the tree.
/// </summary>
public static class TriggerHelpers
{
    /// <summary>
    /// Build a behavior-tree action that calls <see cref="IWorkflowTriggerSink"/>.
    /// Returns Success when the workflow starts (sink returns a run id),
    /// Failure when suppressed (dedup hit) or when the sink throws.
    /// </summary>
    public static IBehaviorTreeNode<TContext> TriggerWorkflow<TContext>(
        string nodeName,
        IWorkflowTriggerSink sink,
        Func<TContext, WorkflowTriggerRequest> requestBuilder) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(requestBuilder);
        return new ActionNode<TContext>(nodeName, async (ctx, ct) =>
        {
            try
            {
                var request = requestBuilder(ctx) ?? throw new InvalidOperationException("requestBuilder returned null.");
                var runId = await sink.TriggerAsync(request, ct).ConfigureAwait(false);
                return runId is null ? BehaviorTreeNodeStatus.Failure : BehaviorTreeNodeStatus.Success;
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                return BehaviorTreeNodeStatus.Failure;
            }
        });
    }

    /// <summary>
    /// Build a behavior-tree action that calls <see cref="IQueueTriggerSink"/>.
    /// Same Success/Failure semantics as <see cref="TriggerWorkflow"/>.
    /// </summary>
    public static IBehaviorTreeNode<TContext> EnqueueWork<TContext>(
        string nodeName,
        IQueueTriggerSink sink,
        Func<TContext, QueueTriggerRequest> requestBuilder) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(requestBuilder);
        return new ActionNode<TContext>(nodeName, async (ctx, ct) =>
        {
            try
            {
                var request = requestBuilder(ctx) ?? throw new InvalidOperationException("requestBuilder returned null.");
                var msgId = await sink.EnqueueAsync(request, ct).ConfigureAwait(false);
                return msgId is null ? BehaviorTreeNodeStatus.Failure : BehaviorTreeNodeStatus.Success;
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                return BehaviorTreeNodeStatus.Failure;
            }
        });
    }

    /// <summary>
    /// Convenience: a SafeTrigger that targets a workflow sink. Combines the
    /// guard pattern from <see cref="ReactivePolicyTemplates.SafeTrigger"/>
    /// with <see cref="TriggerWorkflow"/> in one call. Use this when a tree
    /// needs to "ask the safety policy first, then start the workflow" —
    /// the most common shape per the story acceptance.
    /// </summary>
    public static IBehaviorTreeNode<TContext> GovernedWorkflowTrigger<TContext>(
        string name,
        Func<TContext, bool> guard,
        IWorkflowTriggerSink sink,
        Func<TContext, WorkflowTriggerRequest> requestBuilder) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(guard);
        var sequence = new SequenceNode<TContext>(name);
        sequence.AddChild(new ConditionNode<TContext>($"{name}.guard", guard));
        sequence.AddChild(TriggerWorkflow($"{name}.trigger", sink, requestBuilder));
        return sequence;
    }

    /// <summary>
    /// Convenience: GovernedWorkflowTrigger but for a queue sink.
    /// </summary>
    public static IBehaviorTreeNode<TContext> GovernedQueueTrigger<TContext>(
        string name,
        Func<TContext, bool> guard,
        IQueueTriggerSink sink,
        Func<TContext, QueueTriggerRequest> requestBuilder) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(guard);
        var sequence = new SequenceNode<TContext>(name);
        sequence.AddChild(new ConditionNode<TContext>($"{name}.guard", guard));
        sequence.AddChild(EnqueueWork($"{name}.enqueue", sink, requestBuilder));
        return sequence;
    }
}
