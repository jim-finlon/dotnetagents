namespace DotNetAgents.Workflow.Designer;

/// <summary>
/// Service interface for workflow designer operations.
/// </summary>
public interface IWorkflowDesignerService
{
    /// <summary>
    /// Saves a workflow definition.
    /// </summary>
    /// <param name="definition">The workflow definition to save.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The saved workflow definition with generated ID.</returns>
    Task<WorkflowDefinitionDto> SaveWorkflowAsync(
        WorkflowDefinitionDto definition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workflow definition by ID.
    /// </summary>
    /// <param name="workflowId">The workflow ID.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The workflow definition, or null if not found.</returns>
    Task<WorkflowDefinitionDto?> GetWorkflowAsync(
        string workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all workflow definitions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of workflow definitions.</returns>
    Task<IReadOnlyList<WorkflowDefinitionDto>> ListWorkflowsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a workflow definition.
    /// </summary>
    /// <param name="definition">The workflow definition to validate.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Validation result with any errors or warnings.</returns>
    Task<WorkflowValidationResult> ValidateWorkflowAsync(
        WorkflowDefinitionDto definition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a workflow definition.
    /// </summary>
    /// <param name="workflowId">The workflow ID to execute.</param>
    /// <param name="initialState">Optional initial state for the workflow.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The execution ID for tracking.</returns>
    Task<string> ExecuteWorkflowAsync(
        string workflowId,
        object? initialState = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the execution status.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The execution status, or null if not found.</returns>
    Task<WorkflowExecutionDto?> GetExecutionStatusAsync(
        string executionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of workflow validation.
/// </summary>
public class WorkflowValidationResult
{
    /// <summary>
    /// Gets or sets whether the workflow is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of validation warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
