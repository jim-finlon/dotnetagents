using DotNetAgents.Abstractions.CounterAgents;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Core.CounterAgents;

/// <summary>
/// Runs registered counter-agents in parallel against a proposed action and aggregates the
/// results into a single <see cref="CounterAgentAggregateVerdict"/>. The middleware never
/// throws on counter-agent exceptions — a misbehaving counter-agent is reported as a Concern
/// with the exception message in the reasons list, so the action loop is never silently
/// halted by counter-agent bugs.
/// </summary>
public sealed class CounterAgentMiddleware
{
    private readonly ICounterAgentVerdictAggregator _aggregator;
    private readonly ILogger<CounterAgentMiddleware>? _logger;

    public CounterAgentMiddleware(
        ICounterAgentVerdictAggregator? aggregator = null,
        ILogger<CounterAgentMiddleware>? logger = null)
    {
        _aggregator = aggregator ?? new CounterAgentVerdictAggregator();
        _logger = logger;
    }

    /// <summary>
    /// Review a proposed action with the supplied counter-agents.
    /// </summary>
    /// <remarks>
    /// All counter-agents run concurrently via <see cref="Task.WhenAll(IEnumerable{Task})"/>.
    /// A counter-agent that throws is captured as a synthetic Concern verdict with severity
    /// <see cref="CounterAgentSeverity.Minor"/>; the cancellation token is honored — if the
    /// caller cancels, in-flight reviews receive the cancellation but the aggregate returns
    /// whatever verdicts completed plus synthetic Concerns for the cancelled ones.
    /// </remarks>
    public async Task<CounterAgentAggregateVerdict> ReviewAsync(
        CounterAgentActionProposal proposal,
        IEnumerable<ICounterAgent> counterAgents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentNullException.ThrowIfNull(counterAgents);

        var counterAgentList = counterAgents as IReadOnlyList<ICounterAgent> ?? counterAgents.ToArray();

        if (counterAgentList.Count == 0)
        {
            return _aggregator.Aggregate(Array.Empty<CounterAgentVerdict>());
        }

        var tasks = counterAgentList.Select(ca => RunSingleAsync(ca, proposal, cancellationToken)).ToArray();
        var verdicts = await Task.WhenAll(tasks).ConfigureAwait(false);

        return _aggregator.Aggregate(verdicts);
    }

    private async Task<CounterAgentVerdict> RunSingleAsync(
        ICounterAgent counterAgent,
        CounterAgentActionProposal proposal,
        CancellationToken cancellationToken)
    {
        try
        {
            return await counterAgent.ReviewAsync(proposal, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug(
                "Counter-agent {CounterAgentId} cancelled mid-review for {ActionType}/{ActionName}.",
                counterAgent.Id, proposal.ActionType, proposal.ActionName);
            return CounterAgentVerdict.Concern(
                counterAgent.Id,
                new[] { $"Counter-agent review cancelled before completion." },
                CounterAgentSeverity.Trivial);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "Counter-agent {CounterAgentId} threw during review of {ActionType}/{ActionName}; surfaced as Concern.",
                counterAgent.Id, proposal.ActionType, proposal.ActionName);
            return CounterAgentVerdict.Concern(
                counterAgent.Id,
                new[] { $"Counter-agent threw: {ex.GetType().Name}: {ex.Message}" },
                CounterAgentSeverity.Minor);
        }
    }
}
