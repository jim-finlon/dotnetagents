// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.ModelRouting;

/// <summary>Router that tracks estimated cost and optionally enforces a budget. MR-3.4.</summary>
public sealed class CostAwareRouter : IModelRouter
{
    private readonly IModelRouter _inner;
    private readonly ICostTracker _tracker;
    private readonly Func<RoutingRequest, RoutingResult, decimal>? _estimateCost;
    private readonly decimal? _budget;

    /// <summary>Wraps a router with cost tracking and optional budget.</summary>
    /// <param name="inner">Underlying router.</param>
    /// <param name="tracker">Cost tracker.</param>
    /// <param name="estimateCost">Optional: estimates cost for (request, result). When null, EstimatedCost on result is not set and nothing is recorded.</param>
    /// <param name="budget">Optional: when total cost exceeds this, result is marked OverBudget.</param>
    public CostAwareRouter(
        IModelRouter inner,
        ICostTracker tracker,
        Func<RoutingRequest, RoutingResult, decimal>? estimateCost = null,
        decimal? budget = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _estimateCost = estimateCost;
        _budget = budget;
    }

    /// <inheritdoc />
    public async Task<RoutingResult> RouteAsync(RoutingRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _inner.RouteAsync(request, cancellationToken).ConfigureAwait(false);
        if (_estimateCost == null)
            return result;
        var cost = _estimateCost(request, result);
        _tracker.Record(result.ModelId, cost);
        var overBudget = _budget.HasValue && _tracker.GetTotalCost() > _budget.Value;
        return result with { EstimatedCost = cost, OverBudget = overBudget };
    }
}
