namespace DotNetAgents.Abstractions.Hooks;

/// <summary>
/// A deterministic interceptor that runs at one or more agent-loop lifecycle checkpoints.
/// Hooks let operators inject policy without modifying agent code — block dangerous tool
/// calls, redact sensitive outputs, enforce CI gates, capture evidence, validate constraints.
/// </summary>
/// <remarks>
/// <para>
/// Hooks MUST be deterministic where possible (rule-based) and bounded in latency (typically
/// &lt;5ms p99 in the no-op case, &lt;50ms in a typical chain of 3-5 hooks). LLM-backed hooks
/// are permitted but should defer to PostLLM checkpoints, never PreLLM (which would block on
/// the very call the hook needs to consult).
/// </para>
/// <para>
/// A hook that throws is treated as Allow by default (so misbehaving hooks never silently
/// halt the agent loop) — the exception is captured as evidence. Operators can opt in to
/// "Block on hook failure" via a chain configuration if they want strict failure semantics.
/// </para>
/// </remarks>
public interface IAgentHook
{
    /// <summary>Stable identifier for this hook (used for evidence, dashboards, audit overrides).</summary>
    string Id { get; }

    /// <summary>Operator-readable name displayed in dashboards.</summary>
    string DisplayName { get; }

    /// <summary>The checkpoints this hook subscribes to. The chain executor only invokes the hook at these points.</summary>
    IReadOnlySet<HookCheckpoint> SubscribedCheckpoints { get; }

    /// <summary>Priority within the chain (lower runs first). Convention: framework hooks 0-99, operator hooks 100-999, debug hooks 1000+.</summary>
    int Priority { get; }

    /// <summary>
    /// Evaluate the lifecycle event and return a decision. Implementations SHOULD inspect only
    /// the relevant subset of <paramref name="context"/> for their checkpoint and return Allow
    /// quickly when no policy applies.
    /// </summary>
    Task<HookDecision> EvaluateAsync(
        AgentHookContext context,
        CancellationToken cancellationToken = default);
}
