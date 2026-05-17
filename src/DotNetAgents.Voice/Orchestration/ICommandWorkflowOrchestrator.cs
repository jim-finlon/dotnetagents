using DotNetAgents.Voice.StateMachines;

namespace DotNetAgents.Voice.Orchestration;

/// <summary>
/// Interface for orchestrating voice command workflows.
/// </summary>
public interface ICommandWorkflowOrchestrator
{
    /// <summary>
    /// Executes a command workflow starting from the given state.
    /// </summary>
    /// <param name="state">The initial command state.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The final command state after execution.</returns>
    Task<CommandState> ExecuteAsync(
        CommandState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current voice session state for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The current voice session context, or null if no active session exists.</returns>
    VoiceSessionContext? GetSessionState(Guid userId);
}
