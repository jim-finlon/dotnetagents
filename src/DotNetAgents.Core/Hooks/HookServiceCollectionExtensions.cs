using DotNetAgents.Abstractions.Hooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Core.Hooks;

/// <summary>
/// DI registration helpers for the agent-hook framework. Story f15d03e3 — Phase 2C agent-OS
/// keystone primitive #3.
/// </summary>
public static class HookServiceCollectionExtensions
{
    /// <summary>
    /// Register the hook framework with sensible defaults: chain executor, plus the three
    /// reference hooks (BudgetEnforcement, RedactSecrets, EvidenceCapture in-memory).
    /// Operators can register additional hooks via <see cref="AddAgentHook{T}"/>.
    /// </summary>
    public static IServiceCollection AddAgentHookFramework(
        this IServiceCollection services,
        decimal defaultLlmBudgetUsd = 1.00m)
    {
        services.TryAddSingleton<HookChainExecutor>();

        services.AddSingleton<IAgentHook>(_ => new BudgetEnforcementHook(defaultLlmBudgetUsd));
        services.AddSingleton<IAgentHook>(_ => new RedactSecretsHook());
        services.AddSingleton<IAgentHook>(_ => new EvidenceCaptureHook());

        return services;
    }

    /// <summary>Register a custom hook implementation.</summary>
    public static IServiceCollection AddAgentHook<THook>(this IServiceCollection services)
        where THook : class, IAgentHook
    {
        services.AddSingleton<IAgentHook, THook>();
        return services;
    }

    /// <summary>Register a hook via factory.</summary>
    public static IServiceCollection AddAgentHook(
        this IServiceCollection services,
        Func<IServiceProvider, IAgentHook> factory)
    {
        services.AddSingleton<IAgentHook>(factory);
        return services;
    }

    /// <summary>Register the EvidenceCaptureHook with a production persistence callback (typically writing to SDLC contribution_entry ledger).</summary>
    public static IServiceCollection AddEvidenceCaptureHook(
        this IServiceCollection services,
        Func<CapturedEvidenceEntry, CancellationToken, Task> persistAsync)
    {
        services.AddSingleton<IAgentHook>(_ => new EvidenceCaptureHook(persistAsync));
        return services;
    }
}
