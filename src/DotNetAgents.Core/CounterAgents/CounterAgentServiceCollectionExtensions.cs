using DotNetAgents.Abstractions.CounterAgents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Core.CounterAgents;

/// <summary>
/// DI registration helpers for the counter-agent framework primitive.
/// Story 63686216 — Phase 2A keystone.
/// </summary>
public static class CounterAgentServiceCollectionExtensions
{
    /// <summary>
    /// Register the counter-agent framework with sensible defaults: aggregator, middleware,
    /// and the Budget + Safety reference counter-agents (the two operators almost always
    /// want active by default). Policy and Consistency counter-agents must be added
    /// explicitly via <see cref="AddPolicyCounterAgent"/> / <see cref="AddConsistencyCounterAgent"/>
    /// since their behavior depends on operator-curated policy lists.
    /// </summary>
    public static IServiceCollection AddCounterAgentFramework(
        this IServiceCollection services,
        decimal defaultBudgetUsd = 1.00m)
    {
        services.TryAddSingleton<ICounterAgentVerdictAggregator, CounterAgentVerdictAggregator>();
        services.TryAddSingleton<CounterAgentMiddleware>();

        services.AddSingleton<ICounterAgent>(_ => new BudgetCounterAgent(defaultBudgetUsd));
        services.AddSingleton<ICounterAgent>(_ => new SafetyCounterAgent());

        return services;
    }

    /// <summary>
    /// Register a custom <see cref="ICounterAgent"/> implementation as an additional counter-agent.
    /// Multiple registrations stack — the middleware consults all registered counter-agents in parallel.
    /// </summary>
    public static IServiceCollection AddCounterAgent<TCounterAgent>(this IServiceCollection services)
        where TCounterAgent : class, ICounterAgent
    {
        services.AddSingleton<ICounterAgent, TCounterAgent>();
        return services;
    }

    /// <summary>
    /// Register an <see cref="ICounterAgent"/> instance via factory. Useful for counter-agents
    /// requiring constructor parameters.
    /// </summary>
    public static IServiceCollection AddCounterAgent(
        this IServiceCollection services,
        Func<IServiceProvider, ICounterAgent> factory)
    {
        services.AddSingleton<ICounterAgent>(factory);
        return services;
    }

    /// <summary>
    /// Register the rule-based <see cref="PolicyCounterAgent"/> with operator-curated allow/disallow lists.
    /// </summary>
    public static IServiceCollection AddPolicyCounterAgent(
        this IServiceCollection services,
        IEnumerable<string>? allowedActionTypes = null,
        IEnumerable<string>? disallowedActionTypes = null)
    {
        var allowed = allowedActionTypes?.ToArray();
        var disallowed = disallowedActionTypes?.ToArray();
        services.AddSingleton<ICounterAgent>(_ => new PolicyCounterAgent(allowed, disallowed));
        return services;
    }

    /// <summary>
    /// Register the rule-based <see cref="ConsistencyCounterAgent"/> with operator-curated required-keys-by-action-type.
    /// Pass <c>null</c> to use <see cref="ConsistencyCounterAgent.DefaultRequiredKeysByActionType"/>.
    /// </summary>
    public static IServiceCollection AddConsistencyCounterAgent(
        this IServiceCollection services,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? requiredKeysByActionType = null)
    {
        services.AddSingleton<ICounterAgent>(_ => new ConsistencyCounterAgent(requiredKeysByActionType));
        return services;
    }
}
