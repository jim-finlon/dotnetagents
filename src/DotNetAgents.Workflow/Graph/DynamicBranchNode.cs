using DotNetAgents.Abstractions.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// A workflow node that dynamically selects the next node to execute based on runtime state.
/// The selected node name is stored in state and can be used with conditional edges.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class DynamicBranchNode<TState> : GraphNode<TState> where TState : class
{
    private readonly Func<TState, CancellationToken, Task<string>> _branchSelector;
    private readonly string _nextNodePropertyName;
    private readonly ILogger<DynamicBranchNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicBranchNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the dynamic branch node.</param>
    /// <param name="branchSelector">A function that selects the next node name based on the current state.</param>
    /// <param name="nextNodePropertyName">The name of the property to store the selected next node name. Default is "NextNode".</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public DynamicBranchNode(
        string name,
        Func<TState, CancellationToken, Task<string>> branchSelector,
        string nextNodePropertyName = "NextNode",
        ILogger<DynamicBranchNode<TState>>? logger = null)
        : base(name, CreateHandler(
            branchSelector ?? throw new ArgumentNullException(nameof(branchSelector)),
            nextNodePropertyName ?? throw new ArgumentNullException(nameof(nextNodePropertyName)),
            logger,
            name))
    {
        _branchSelector = branchSelector;
        _nextNodePropertyName = nextNodePropertyName;
        _logger = logger;
        Description = "Dynamically selects next node based on state";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicBranchNode{TState}"/> class with a synchronous branch selector.
    /// </summary>
    /// <param name="name">The name of the dynamic branch node.</param>
    /// <param name="branchSelector">A function that selects the next node name based on the current state.</param>
    /// <param name="nextNodePropertyName">The name of the property to store the selected next node name. Default is "NextNode".</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public DynamicBranchNode(
        string name,
        Func<TState, string> branchSelector,
        string nextNodePropertyName = "NextNode",
        ILogger<DynamicBranchNode<TState>>? logger = null)
        : this(
            name,
            (state, ct) => Task.FromResult(branchSelector(state)),
            nextNodePropertyName,
            logger)
    {
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        Func<TState, CancellationToken, Task<string>> branchSelector,
        string nextNodePropertyName,
        ILogger<DynamicBranchNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            logger?.LogDebug(
                "Node {NodeName}: Evaluating branch selector to determine next node.",
                nodeName);

            // Execute branch selector to determine next node
            string nextNode;
            try
            {
                nextNode = await branchSelector(state, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error in branch selector for node '{NodeName}'.", nodeName);
                throw new AgentException(
                    $"Branch selector failed for node '{nodeName}': {ex.Message}",
                    ErrorCategory.WorkflowError,
                    ex);
            }

            if (string.IsNullOrWhiteSpace(nextNode))
            {
                throw new AgentException(
                    $"Branch selector returned null or empty node name for node '{nodeName}'.",
                    ErrorCategory.WorkflowError);
            }

            logger?.LogInformation(
                "Node {NodeName}: Selected next node '{NextNode}'.",
                nodeName,
                nextNode);

            // Store the selected next node in state
            SetNextNodeInState(state, nextNodePropertyName, nextNode);

            return state;
        };
    }

    private static void SetNextNodeInState(TState state, string propertyName, string nextNode)
    {
        var type = typeof(TState);
        var prop = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (prop != null && prop.CanWrite)
        {
            try
            {
                // Try to set as string
                if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(state, nextNode);
                }
                // Try to set as object
                else if (prop.PropertyType == typeof(object))
                {
                    prop.SetValue(state, nextNode);
                }
            }
            catch (Exception)
            {
                // Ignore if we can't set the property - will be handled by conditional edges
            }
        }
    }

    /// <summary>
    /// Creates a conditional edge function that checks if the next node matches a specific node name.
    /// This can be used with WorkflowBuilder.AddEdge to create conditional edges based on dynamic branch selection.
    /// </summary>
    /// <param name="targetNodeName">The node name to check for.</param>
    /// <param name="nextNodePropertyName">The property name that stores the next node. Default is "NextNode".</param>
    /// <returns>A condition function that returns true if the next node matches the target.</returns>
    public static Func<TState, bool> CreateConditionalEdge(string targetNodeName, string nextNodePropertyName = "NextNode")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetNodeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(nextNodePropertyName);

        return (state) =>
        {
            if (state == null)
                return false;

            var type = typeof(TState);
            var prop = type.GetProperty(nextNodePropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (prop == null)
                return false;

            try
            {
                var value = prop.GetValue(state);
                if (value is string strValue)
                {
                    return strValue.Equals(targetNodeName, StringComparison.OrdinalIgnoreCase);
                }
                return value?.ToString()?.Equals(targetNodeName, StringComparison.OrdinalIgnoreCase) == true;
            }
            catch
            {
                return false;
            }
        };
    }
}
