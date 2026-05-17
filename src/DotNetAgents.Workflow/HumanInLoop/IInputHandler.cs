namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// Interface for handling human input requests in workflows.
/// Input requests allow workflows to request specific data from humans.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public interface IInputHandler<TState> where TState : class
{
    /// <summary>
    /// Requests input from a human.
    /// </summary>
    /// <typeparam name="TValue">The type of the input value.</typeparam>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the input.</param>
    /// <param name="state">The current workflow state.</param>
    /// <param name="propertyName">The name of the property to set with the input value.</param>
    /// <param name="inputType">The type of input requested.</param>
    /// <param name="prompt">The prompt/question to present to the human.</param>
    /// <param name="defaultValue">Optional default value.</param>
    /// <param name="validationRule">Optional validation rule (e.g., regex pattern, min/max values).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The input value, or null if no input has been provided yet.</returns>
    Task<TValue?> RequestInputAsync<TValue>(
        string workflowRunId,
        string nodeName,
        TState state,
        string propertyName,
        InputType inputType,
        string prompt,
        TValue? defaultValue = default,
        string? validationRule = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if input has been provided for a workflow state transition.
    /// </summary>
    /// <typeparam name="TValue">The type of the input value.</typeparam>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the input.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The input value if provided; otherwise, null.</returns>
    Task<TValue?> GetInputAsync<TValue>(
        string workflowRunId,
        string nodeName,
        string propertyName,
        CancellationToken cancellationToken = default);
}
