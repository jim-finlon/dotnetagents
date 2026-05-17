using DotNetAgents.Abstractions.Chains;

namespace DotNetAgents.AgentFramework.Adapters;

/// <summary>
/// Adapter interface for converting DotNetAgents chains to Microsoft Agent Framework workflows.
/// </summary>
/// <remarks>
/// This interface will be implemented when Microsoft Agent Framework APIs stabilize.
/// It provides a bridge between DotNetAgents chain patterns and MAF workflow patterns.
/// </remarks>
public interface IChainAdapter
{
    /// <summary>
    /// Converts a DotNetAgents chain to a MAF-compatible workflow.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <param name="chain">The DotNetAgents chain to convert.</param>
    /// <returns>A MAF-compatible workflow representation.</returns>
    object ConvertToMAFWorkflow<TInput, TOutput>(IRunnable<TInput, TOutput> chain);

    /// <summary>
    /// Creates a MAF workflow from DotNetAgents chain components.
    /// </summary>
    /// <param name="components">The chain components to compose.</param>
    /// <returns>A MAF-compatible workflow.</returns>
    object CreateWorkflowFromComponents(IEnumerable<object> components);
}
