using DotNetAgents.Abstractions.Hooks;

namespace DotNetAgents.Core.Hooks;

/// <summary>
/// Reference hook that blocks a PreLlmCall when the projected cost exceeds the configured
/// per-call budget. Reads <c>cost_estimate_usd</c> from context metadata; absence is treated
/// as Allow (no estimate, no opinion). Pairs naturally with the BudgetCounterAgent — both
/// enforce cost discipline but at different layers (hook = pre-call gate at the framework
/// layer; counter-agent = action-proposal review at the substrate layer).
/// </summary>
public sealed class BudgetEnforcementHook : IAgentHook
{
    private static readonly IReadOnlySet<HookCheckpoint> _checkpoints =
        new HashSet<HookCheckpoint> { HookCheckpoint.PreLlmCall };

    private readonly decimal _defaultBudgetUsd;

    public BudgetEnforcementHook(decimal defaultBudgetUsd = 1.00m)
    {
        if (defaultBudgetUsd <= 0m) throw new ArgumentOutOfRangeException(nameof(defaultBudgetUsd));
        _defaultBudgetUsd = defaultBudgetUsd;
    }

    public string Id => "dotnetagents.budget-enforcement-hook";
    public string DisplayName => "Budget Enforcement Hook";
    public IReadOnlySet<HookCheckpoint> SubscribedCheckpoints => _checkpoints;
    public int Priority => 10;

    public Task<HookDecision> EvaluateAsync(
        AgentHookContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Checkpoint != HookCheckpoint.PreLlmCall)
        {
            return Task.FromResult<HookDecision>(HookDecision.Allow);
        }

        var costEstimate = context.GetMetadataValue<decimal?>("cost_estimate_usd");
        if (costEstimate is null || costEstimate <= 0m)
        {
            return Task.FromResult<HookDecision>(HookDecision.Allow);
        }

        var budget = context.GetMetadataValue<decimal?>("budget_ceiling_usd") ?? _defaultBudgetUsd;
        if (costEstimate <= budget)
        {
            return Task.FromResult<HookDecision>(HookDecision.Allow);
        }

        return Task.FromResult<HookDecision>(HookDecision.BlockedBecause(
            new[]
            {
                $"Projected LLM call cost ${costEstimate.Value:F4} exceeds budget ${budget:F4} for actor '{context.ActorId}'.",
            },
            Id));
    }
}
