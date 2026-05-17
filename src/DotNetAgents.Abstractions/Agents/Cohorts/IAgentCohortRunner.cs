namespace DotNetAgents.Abstractions.Agents.Cohorts;

/// <summary>
/// Runs a coordinated cohort of agent instances against one shared task.
/// </summary>
/// <typeparam name="TAgent">The concrete agent type.</typeparam>
/// <typeparam name="TConfiguration">The caller-defined configuration snapshot type.</typeparam>
public interface IAgentCohortRunner<TAgent, TConfiguration>
    where TAgent : IAgent
{
    /// <summary>
    /// Runs the cohort and returns an evidence bundle for every executed member.
    /// </summary>
    /// <param name="definition">The cohort definition.</param>
    /// <param name="instanceFactory">Factory used to create member runtime instances.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The completed cohort run result.</returns>
    ValueTask<AgentCohortRunResult> RunAsync(
        AgentCohortDefinition<TConfiguration> definition,
        IAgentInstanceFactory<TAgent, TConfiguration> instanceFactory,
        CancellationToken cancellationToken = default);
}
