namespace DotNetAgents.Abstractions.CounterAgents;

/// <summary>
/// A counter-agent — an adversarial reviewer that evaluates proposed actions before they
/// execute. Counter-agents are part of DNA's substrate-level "balance of power" model: every
/// agent action that crosses a sensitive boundary (tool call, deploy, story close) gets
/// reviewed by registered counter-agents in parallel; verdicts become first-class evidence.
/// </summary>
/// <remarks>
/// <para>
/// Implementations should be deterministic where possible (rule-based) and stateless across
/// review calls. LLM-backed counter-agents are allowed but must respect bounded latency
/// (typically &lt;10s) since they sit on the action hot path.
/// </para>
/// <para>
/// A counter-agent that throws is treated as a non-decision and logged; the middleware never
/// fails the action loop because of a misbehaving counter-agent. This is intentional — a buggy
/// counter-agent must never lock the platform.
/// </para>
/// </remarks>
public interface ICounterAgent
{
    /// <summary>Stable id of this counter-agent (used for telemetry, dashboard grouping, override audit).</summary>
    string Id { get; }

    /// <summary>Operator-readable name displayed in dashboards.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Review a proposed action and return a verdict. Implementations MUST return a verdict;
    /// thrown exceptions are caught by the middleware and treated as a Concern with the
    /// exception message as a reason (so misbehaving counter-agents do not silently fail
    /// the action loop, and operators see the failure as evidence).
    /// </summary>
    Task<CounterAgentVerdict> ReviewAsync(
        CounterAgentActionProposal proposal,
        CancellationToken cancellationToken = default);
}
