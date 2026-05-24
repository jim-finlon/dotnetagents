// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.CounterAgents;

namespace DotNetAgents.Core.CounterAgents;

/// <summary>
/// Reference counter-agent that flags actions exceeding a configured per-call cost budget.
/// Reads <c>cost_estimate_usd</c> from the proposal's metadata; if absent, returns Approve
/// (no estimate, no opinion). If present and over budget, returns Block with severity scaled
/// by overage ratio.
/// </summary>
/// <remarks>
/// Deterministic, rule-based, zero-LLM. Drop-in default for any agent. Budget can be configured
/// per-instance or read from proposal metadata key <c>budget_ceiling_usd</c> for per-action override.
/// </remarks>
public sealed class BudgetCounterAgent : ICounterAgent
{
    private readonly decimal _defaultBudgetUsd;

    public BudgetCounterAgent(decimal defaultBudgetUsd = 1.00m)
    {
        if (defaultBudgetUsd <= 0m) throw new ArgumentOutOfRangeException(nameof(defaultBudgetUsd), "Budget must be positive.");
        _defaultBudgetUsd = defaultBudgetUsd;
    }

    /// <inheritdoc />
    public string Id => "dotnetagents.budget-counter-agent";

    /// <inheritdoc />
    public string DisplayName => "Budget Counter-Agent";

    /// <inheritdoc />
    public Task<CounterAgentVerdict> ReviewAsync(
        CounterAgentActionProposal proposal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        cancellationToken.ThrowIfCancellationRequested();

        var costEstimate = proposal.GetMetadataValue<decimal?>("cost_estimate_usd");
        if (costEstimate is null || costEstimate <= 0m)
        {
            return Task.FromResult(CounterAgentVerdict.Approve(Id));
        }

        var budget = proposal.GetMetadataValue<decimal?>("budget_ceiling_usd") ?? _defaultBudgetUsd;
        if (costEstimate <= budget)
        {
            return Task.FromResult(CounterAgentVerdict.Approve(Id));
        }

        var overageRatio = (double)(costEstimate.Value / budget);
        var severity = overageRatio switch
        {
            > 10.0 => CounterAgentSeverity.Critical,
            > 3.0 => CounterAgentSeverity.Major,
            > 1.5 => CounterAgentSeverity.Moderate,
            _ => CounterAgentSeverity.Minor,
        };

        var reason =
            $"Estimated cost ${costEstimate.Value:F4} exceeds budget ${budget:F4} " +
            $"by {overageRatio:F2}x for {proposal.ActionType}/{proposal.ActionName}.";

        return Task.FromResult(CounterAgentVerdict.Block(
            Id,
            new[] { reason },
            severity,
            metadata: new Dictionary<string, object>
            {
                ["cost_estimate_usd"] = costEstimate.Value,
                ["budget_ceiling_usd"] = budget,
                ["overage_ratio"] = overageRatio,
            }));
    }
}
