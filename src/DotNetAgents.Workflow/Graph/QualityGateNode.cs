// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// Workflow node that evaluates state with an <see cref="IQualityGate{TState}"/> and stores the result
/// in state for conditional routing. Add conditional edges from this node (e.g. when Decision is Pass vs Fail).
/// </summary>
/// <typeparam name="TState">The workflow state type.</typeparam>
public class QualityGateNode<TState> : GraphNode<TState> where TState : class
{
    /// <summary>
    /// Creates a quality gate node that evaluates state and stores the result using the provided delegate.
    /// </summary>
    /// <param name="name">Node name.</param>
    /// <param name="qualityGate">The quality gate to evaluate.</param>
    /// <param name="storeResult">Called to persist the result into state (e.g. set state.Metadata["quality_gate_decision"]).</param>
    /// <param name="logger">Optional logger.</param>
    public QualityGateNode(
        string name,
        IQualityGate<TState> qualityGate,
        Func<TState, QualityGateResult, TState> storeResult,
        ILogger<QualityGateNode<TState>>? logger = null)
        : base(name, CreateHandler(qualityGate ?? throw new ArgumentNullException(nameof(qualityGate)),
            storeResult ?? throw new ArgumentNullException(nameof(storeResult)),
            logger,
            name))
    {
        Description = "Evaluates quality gate and stores result for routing";
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        IQualityGate<TState> qualityGate,
        Func<TState, QualityGateResult, TState> storeResult,
        ILogger<QualityGateNode<TState>>? logger,
        string nodeName)
    {
        return (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            logger?.LogInformation("Node {NodeName}: Evaluating quality gate", nodeName);
            var result = qualityGate.Evaluate(state);
            logger?.LogInformation("Node {NodeName}: Quality gate decision = {Decision}", nodeName, result.Decision);

            var updated = storeResult(state, result);
            return Task.FromResult(updated);
        };
    }
}
