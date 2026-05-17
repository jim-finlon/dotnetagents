using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Workflow.Graph;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// A workflow node that requires human approval before proceeding.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class ApprovalNode<TState> : GraphNode<TState> where TState : class
{
    private readonly IApprovalHandler<TState> _approvalHandler;
    private readonly string? _approvalMessage;
    private readonly TimeSpan? _timeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the approval node.</param>
    /// <param name="approvalHandler">The approval handler to use.</param>
    /// <param name="approvalMessage">Optional message to display when requesting approval.</param>
    /// <param name="timeout">Optional timeout for approval. If null, waits indefinitely.</param>
    public ApprovalNode(
        string name,
        IApprovalHandler<TState> approvalHandler,
        string? approvalMessage = null,
        TimeSpan? timeout = null)
        : base(name, CreateHandler(approvalHandler ?? throw new ArgumentNullException(nameof(approvalHandler)), name, approvalMessage, timeout))
    {
        _approvalHandler = approvalHandler;
        _approvalMessage = approvalMessage;
        _timeout = timeout;
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        IApprovalHandler<TState> approvalHandler,
        string nodeName,
        string? approvalMessage,
        TimeSpan? timeout)
    {
        return async (state, ct) =>
        {
            var workflowRunId = GetWorkflowRunId(state) ?? Guid.NewGuid().ToString("N");

            // Request approval
            var approved = await approvalHandler.RequestApprovalAsync(
                workflowRunId,
                nodeName,
                state,
                approvalMessage,
                ct).ConfigureAwait(false);

            if (!approved)
            {
                // Wait for approval if not immediately granted
                if (timeout.HasValue)
                {
                    using var timeoutCts = new CancellationTokenSource(timeout.Value);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                    var startTime = DateTime.UtcNow;
                    while (DateTime.UtcNow - startTime < timeout.Value)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token).ConfigureAwait(false);
                        approved = await approvalHandler.IsApprovedAsync(workflowRunId, nodeName, linkedCts.Token).ConfigureAwait(false);
                        if (approved)
                        {
                            break;
                        }
                    }

                    if (!approved)
                    {
                        throw new AgentException(
                            $"Approval timeout after {timeout.Value.TotalSeconds} seconds for node '{nodeName}'.",
                            ErrorCategory.WorkflowError);
                    }
                }
                else
                {
                    // Poll for approval
                    while (!approved)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);
                        approved = await approvalHandler.IsApprovedAsync(workflowRunId, nodeName, ct).ConfigureAwait(false);
                    }
                }
            }

            // Check if approval was actually granted
            approved = await approvalHandler.IsApprovedAsync(workflowRunId, nodeName, ct).ConfigureAwait(false);
            if (!approved)
            {
                throw new AgentException(
                    $"Approval was rejected for node '{nodeName}'.",
                    ErrorCategory.WorkflowError);
            }

            return state;
        };
    }

    private static string? GetWorkflowRunId(TState state)
    {
        // Try to get workflow run ID from state if it has a property
        var type = typeof(TState);
        var prop = type.GetProperty("WorkflowRunId") ?? type.GetProperty("RunId");
        if (prop != null)
        {
            return prop.GetValue(state)?.ToString();
        }

        return null;
    }
}
