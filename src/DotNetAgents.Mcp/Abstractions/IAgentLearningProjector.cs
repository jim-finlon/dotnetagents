using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Abstractions;

public interface IAgentLearningProjector
{
    Task<AgentLearningProjectionResult> ProjectAsync(
        LearningEventV1 learningEvent,
        CancellationToken cancellationToken = default);
}
