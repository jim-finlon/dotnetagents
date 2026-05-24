// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.BehaviorTrees;

namespace DotNetAgents.Agents.ControlLoops;

/// <summary>
/// Reusable behavior-tree policy templates that DNA services compose into
/// their reactive trees instead of hand-assembling the same patterns.
/// Story f4dd9eb4. Each template returns a behavior-tree node — callers slot
/// the result into their own tree wherever they need that policy.
///
/// Templates are intentionally thin compositions over the existing leaf and
/// composite primitives (ActionNode, ConditionNode, SelectorNode,
/// SequenceNode, CooldownNode, RepeaterNode, etc.) — they don't hide tree
/// structure from adopters and they don't embed business workflows.
/// </summary>
public static class ReactivePolicyTemplates
{
    /// <summary>
    /// Retry an action up to <paramref name="maxAttempts"/> times. Returns a
    /// SelectorNode that tries the action; on failure the next attempt fires
    /// after the per-attempt backoff cooldown. Sleep happens *between* attempts,
    /// not before the first one.
    /// </summary>
    public static IBehaviorTreeNode<TContext> RetryWithBackoff<TContext>(
        string name,
        Func<TContext, CancellationToken, Task<BehaviorTreeNodeStatus>> action,
        int maxAttempts,
        Func<int, TimeSpan> backoff) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(backoff);
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts), "maxAttempts must be >= 1");

        var selector = new SelectorNode<TContext>(name);
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            // Capture for closure
            var actionNode = new ActionNode<TContext>($"{name}.attempt-{attempt}", action);
            if (attempt == 1)
            {
                selector.AddChild(actionNode);
            }
            else
            {
                // Wrap in cooldown so this attempt only fires once enough time has passed.
                // Zero/negative backoff means "no spacing" — skip the cooldown wrapper entirely
                // (CooldownNode rejects non-positive durations).
                var delay = backoff(attempt);
                if (delay > TimeSpan.Zero)
                {
                    var cooled = new CooldownNode<TContext>($"{name}.attempt-{attempt}.cooldown", delay);
                    cooled.SetChild(actionNode);
                    selector.AddChild(cooled);
                }
                else
                {
                    selector.AddChild(actionNode);
                }
            }
        }
        return selector;
    }

    /// <summary>
    /// Wrap a child node in a cooldown so it only fires once per period.
    /// Returns Running until the cooldown elapses, then executes the child.
    /// </summary>
    public static IBehaviorTreeNode<TContext> Cooldown<TContext>(
        string name,
        IBehaviorTreeNode<TContext> child,
        TimeSpan period) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(child);
        var node = new CooldownNode<TContext>(name, period);
        node.SetChild(child);
        return node;
    }

    /// <summary>
    /// Watchdog/heartbeat. Sequence: check that <paramref name="isHealthy"/> is
    /// true; if so → run <paramref name="onHealthy"/>. If unhealthy, the
    /// sequence fails and the parent selector can fall through to recovery.
    /// </summary>
    public static IBehaviorTreeNode<TContext> Watchdog<TContext>(
        string name,
        Func<TContext, bool> isHealthy,
        IBehaviorTreeNode<TContext> onHealthy) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(isHealthy);
        ArgumentNullException.ThrowIfNull(onHealthy);

        var sequence = new SequenceNode<TContext>(name);
        sequence.AddChild(new ConditionNode<TContext>($"{name}.isHealthy", isHealthy));
        sequence.AddChild(onHealthy);
        return sequence;
    }

    /// <summary>
    /// Try the primary policy; on failure, fall through to the fallback policy.
    /// </summary>
    public static IBehaviorTreeNode<TContext> Fallback<TContext>(
        string name,
        IBehaviorTreeNode<TContext> primary,
        IBehaviorTreeNode<TContext> fallback) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(fallback);

        var selector = new SelectorNode<TContext>(name);
        selector.AddChild(primary);
        selector.AddChild(fallback);
        return selector;
    }

    /// <summary>
    /// Escalation chain: try a list of policies in order from least-invasive to
    /// most-invasive. The first that returns Success short-circuits the chain.
    /// Use this for event response: notify → page → auto-remediate → kill.
    /// </summary>
    public static IBehaviorTreeNode<TContext> Escalation<TContext>(
        string name,
        params IBehaviorTreeNode<TContext>[] tiers) where TContext : class
    {
        if (tiers is null || tiers.Length == 0)
            throw new ArgumentException("Escalation requires at least one tier.", nameof(tiers));

        var selector = new SelectorNode<TContext>(name);
        foreach (var tier in tiers)
            selector.AddChild(tier ?? throw new ArgumentException("Escalation tier cannot be null."));
        return selector;
    }

    /// <summary>
    /// Stimulus routing: a selector of (condition, handler) pairs. Walks the
    /// pairs and dispatches to the first handler whose condition matches.
    /// Mirrors a switch-on-stimulus shape without hand-coding it each time.
    /// </summary>
    public static IBehaviorTreeNode<TContext> StimulusRouting<TContext>(
        string name,
        params (string Stimulus, Func<TContext, bool> Match, IBehaviorTreeNode<TContext> Handler)[] routes) where TContext : class
    {
        if (routes is null || routes.Length == 0)
            throw new ArgumentException("StimulusRouting requires at least one route.", nameof(routes));

        var selector = new SelectorNode<TContext>(name);
        foreach (var (stimulus, match, handler) in routes)
        {
            ArgumentNullException.ThrowIfNull(match);
            ArgumentNullException.ThrowIfNull(handler);
            var sequence = new SequenceNode<TContext>($"{name}.route.{stimulus}");
            sequence.AddChild(new ConditionNode<TContext>($"{name}.match.{stimulus}", match));
            sequence.AddChild(handler);
            selector.AddChild(sequence);
        }
        return selector;
    }

    /// <summary>
    /// Safe trigger: validate guard → execute downstream trigger. If the guard
    /// fails, the trigger never runs and the node returns Failure. Use this
    /// for "ask the safety policy first, then enqueue the workflow" — it
    /// keeps the entire business workflow OUT of the behavior tree.
    /// </summary>
    public static IBehaviorTreeNode<TContext> SafeTrigger<TContext>(
        string name,
        Func<TContext, bool> guard,
        Func<TContext, CancellationToken, Task<BehaviorTreeNodeStatus>> trigger) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(guard);
        ArgumentNullException.ThrowIfNull(trigger);

        var sequence = new SequenceNode<TContext>(name);
        sequence.AddChild(new ConditionNode<TContext>($"{name}.guard", guard));
        sequence.AddChild(new ActionNode<TContext>($"{name}.trigger", trigger));
        return sequence;
    }
}
