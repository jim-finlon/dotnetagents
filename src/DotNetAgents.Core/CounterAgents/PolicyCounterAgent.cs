using DotNetAgents.Abstractions.CounterAgents;

namespace DotNetAgents.Core.CounterAgents;

/// <summary>
/// Reference counter-agent that enforces an operator-curated allowlist of action types and
/// names. Actions matching the allowlist Approve; un-listed actions Concern (not Block) so
/// the platform can ramp policy without paralyzing dev work.
/// </summary>
/// <remarks>
/// Rule-based MVP. A future LLM-backed implementation can sit alongside this one and consult
/// task-family-specific policies. This rule-based implementation ships now so the framework
/// has a working policy review out of the box.
/// </remarks>
public sealed class PolicyCounterAgent : ICounterAgent
{
    private readonly HashSet<string> _allowedActionTypes;
    private readonly HashSet<string> _disallowedActionTypes;

    public PolicyCounterAgent(
        IEnumerable<string>? allowedActionTypes = null,
        IEnumerable<string>? disallowedActionTypes = null)
    {
        _allowedActionTypes = new HashSet<string>(
            allowedActionTypes ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        _disallowedActionTypes = new HashSet<string>(
            disallowedActionTypes ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string Id => "dotnetagents.policy-counter-agent";

    /// <inheritdoc />
    public string DisplayName => "Policy Counter-Agent";

    /// <inheritdoc />
    public Task<CounterAgentVerdict> ReviewAsync(
        CounterAgentActionProposal proposal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        cancellationToken.ThrowIfCancellationRequested();

        if (_disallowedActionTypes.Contains(proposal.ActionType))
        {
            return Task.FromResult(CounterAgentVerdict.Block(
                Id,
                new[] { $"Action type '{proposal.ActionType}' is in the disallowed-types policy list." },
                CounterAgentSeverity.Critical));
        }

        // If an allowlist is configured, enforce it; otherwise approve any action type.
        if (_allowedActionTypes.Count > 0 && !_allowedActionTypes.Contains(proposal.ActionType))
        {
            return Task.FromResult(CounterAgentVerdict.Concern(
                Id,
                new[] { $"Action type '{proposal.ActionType}' is not in the allowlist (configured types: {string.Join(", ", _allowedActionTypes)})." },
                CounterAgentSeverity.Moderate));
        }

        return Task.FromResult(CounterAgentVerdict.Approve(Id));
    }
}
