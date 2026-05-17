// Story c4b3b3e5 — moved from DotNetAgents.Agents.Supervisor to
// DotNetAgents.Workflow so the workflow-side QualityGateNode can depend on a
// Workflow-owned contract rather than pulling Supervisor into Workflow.
namespace DotNetAgents.Workflow;

/// <summary>
/// Quality gate interface for pipeline checkpoints.
/// Evaluates state and returns a routing decision (Advance, Remediate, Pass, Fail, etc.).
/// Used by EducationAgent (LearnerQualityGate) and PublishingAgent (MikeyGate).
/// </summary>
/// <typeparam name="TState">The orchestrator state type to evaluate.</typeparam>
public interface IQualityGate<TState>
{
    /// <summary>
    /// Evaluates the current state and returns a quality gate decision.
    /// </summary>
    /// <param name="state">The orchestrator state (e.g., after assessment, after review panel).</param>
    /// <returns>The quality gate result with decision and optional targets.</returns>
    QualityGateResult Evaluate(TState state);
}
