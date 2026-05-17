using DotNetAgents.Abstractions.CounterAgents;

namespace DotNetAgents.Core.CounterAgents;

/// <summary>
/// Reference counter-agent that flags actions whose proposal metadata is missing required
/// keys for the action type. Catches integration drift early — e.g. a "deploy" action that
/// forgot to include the deployment_target metadata, or a "tool_call" that forgot the
/// task_category.
/// </summary>
/// <remarks>
/// Required-keys policy is operator-curated by action type. Unknown action types Approve by
/// default (no opinion). Returned verdicts are typically Concern, not Block — consistency
/// drift is rarely safety-critical, but it should surface so dashboards catch it.
/// </remarks>
public sealed class ConsistencyCounterAgent : ICounterAgent
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _requiredKeysByActionType;

    /// <summary>
    /// Default required-keys policy reflecting the conventional metadata keys used by DNA
    /// agents at typical action boundaries. Operators can override at construction.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultRequiredKeysByActionType =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["tool_call"] = new[] { "task_category" },
            ["deploy"] = new[] { "deployment_target", "story_id" },
            ["story_close"] = new[] { "story_id" },
            ["credential_rotate"] = new[] { "credential_category", "credential_name" },
        };

    public ConsistencyCounterAgent(
        IReadOnlyDictionary<string, IReadOnlyList<string>>? requiredKeysByActionType = null)
    {
        _requiredKeysByActionType = requiredKeysByActionType ?? DefaultRequiredKeysByActionType;
    }

    /// <inheritdoc />
    public string Id => "dotnetagents.consistency-counter-agent";

    /// <inheritdoc />
    public string DisplayName => "Consistency Counter-Agent";

    /// <inheritdoc />
    public Task<CounterAgentVerdict> ReviewAsync(
        CounterAgentActionProposal proposal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_requiredKeysByActionType.TryGetValue(proposal.ActionType, out var requiredKeys))
        {
            return Task.FromResult(CounterAgentVerdict.Approve(Id));
        }

        var metadata = proposal.Metadata ?? new Dictionary<string, object>();
        var missingKeys = requiredKeys
            .Where(key => !metadata.ContainsKey(key))
            .ToArray();

        if (missingKeys.Length == 0)
        {
            return Task.FromResult(CounterAgentVerdict.Approve(Id));
        }

        var reasons = missingKeys
            .Select(k => $"Action type '{proposal.ActionType}' is missing required metadata key '{k}'.")
            .ToArray();

        return Task.FromResult(CounterAgentVerdict.Concern(
            Id,
            reasons,
            CounterAgentSeverity.Minor,
            metadata: new Dictionary<string, object>
            {
                ["missing_metadata_keys"] = missingKeys,
                ["action_type"] = proposal.ActionType,
            }));
    }
}
