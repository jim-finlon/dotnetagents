using DotNetAgents.Abstractions.Tools;

namespace DotNetAgents.Abstractions.Agents;

/// <summary>
/// Interface for agents that can use tools and make decisions.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Executes a single step of the agent.
    /// </summary>
    /// <param name="input">The input for this step.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The result of this step.</returns>
    Task<AgentStepResult> ExecuteStepAsync(
        string input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tools available to this agent.
    /// </summary>
    IReadOnlyList<ITool> AvailableTools { get; }
}
