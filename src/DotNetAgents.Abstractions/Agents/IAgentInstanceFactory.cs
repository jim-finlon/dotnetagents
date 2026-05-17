namespace DotNetAgents.Abstractions.Agents;

/// <summary>
/// Creates deliberately named and configured runtime instances for one agent species.
/// </summary>
/// <typeparam name="TAgent">The concrete agent type.</typeparam>
/// <typeparam name="TConfiguration">The caller-defined configuration snapshot type.</typeparam>
public interface IAgentInstanceFactory<TAgent, TConfiguration>
    where TAgent : IAgent
{
    /// <summary>
    /// Creates one configured runtime instance.
    /// </summary>
    /// <param name="request">The instance request.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The configured runtime instance.</returns>
    ValueTask<AgentRuntimeInstance<TAgent, TConfiguration>> CreateAsync(
        AgentInstanceRequest<TConfiguration> request,
        CancellationToken cancellationToken = default);
}
