using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// Extension methods for registering human-in-the-loop services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the in-memory approval handler to the service collection.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryApprovalHandler<TState>(this IServiceCollection services)
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IApprovalHandler<TState>, InMemoryApprovalHandler<TState>>();
        return services;
    }

    /// <summary>
    /// Adds the in-memory decision handler to the service collection.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryDecisionHandler<TState>(this IServiceCollection services)
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IDecisionHandler<TState>, InMemoryDecisionHandler<TState>>();
        return services;
    }

    /// <summary>
    /// Adds custom approval handler implementation to the service collection.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <typeparam name="THandler">The type of the approval handler implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApprovalHandler<TState, THandler>(this IServiceCollection services)
        where TState : class
        where THandler : class, IApprovalHandler<TState>
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IApprovalHandler<TState>, THandler>();
        return services;
    }

    /// <summary>
    /// Adds custom decision handler implementation to the service collection.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <typeparam name="THandler">The type of the decision handler implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDecisionHandler<TState, THandler>(this IServiceCollection services)
        where TState : class
        where THandler : class, IDecisionHandler<TState>
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IDecisionHandler<TState>, THandler>();
        return services;
    }

    /// <summary>
    /// Adds the in-memory input handler to the service collection.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryInputHandler<TState>(this IServiceCollection services)
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IInputHandler<TState>, InMemoryInputHandler<TState>>();
        return services;
    }

    /// <summary>
    /// Adds custom input handler implementation to the service collection.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <typeparam name="THandler">The type of the input handler implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInputHandler<TState, THandler>(this IServiceCollection services)
        where TState : class
        where THandler : class, IInputHandler<TState>
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IInputHandler<TState>, THandler>();
        return services;
    }

    /// <summary>
    /// Adds the in-memory review handler to the service collection.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryReviewHandler<TState>(this IServiceCollection services)
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IReviewHandler<TState>, InMemoryReviewHandler<TState>>();
        return services;
    }

    /// <summary>
    /// Adds custom review handler implementation to the service collection.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <typeparam name="THandler">The type of the review handler implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddReviewHandler<TState, THandler>(this IServiceCollection services)
        where TState : class
        where THandler : class, IReviewHandler<TState>
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IReviewHandler<TState>, THandler>();
        return services;
    }
}
