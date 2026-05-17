namespace DotNetAgents.A2A;

/// <summary>A2A agent: card and task handling. FR-A2A-001, FR-A2A-002.</summary>
public interface IA2AAgent
{
    /// <summary>Returns the agent card (capabilities, skills).</summary>
    AgentCard GetAgentCard();

    /// <summary>Handles the task and returns a single response.</summary>
    Task<A2AResponse> HandleTaskAsync(A2ATask task, CancellationToken cancellationToken = default);

    /// <summary>Handles the task with streaming events.</summary>
    IAsyncEnumerable<A2AEvent> StreamTaskAsync(A2ATask task, CancellationToken cancellationToken = default);
}
