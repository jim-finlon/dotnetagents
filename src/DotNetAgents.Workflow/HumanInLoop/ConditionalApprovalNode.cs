using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// Approval outcome enumeration for conditional branching.
/// </summary>
public enum ApprovalOutcome
{
    /// <summary>
    /// Approval is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Approval was granted.
    /// </summary>
    Approved,

    /// <summary>
    /// Approval was rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// Approval was modified (state was changed during review).
    /// </summary>
    Modified
}

/// <summary>
/// A workflow node that requires human approval and stores the outcome in state for conditional branching.
/// Unlike <see cref="ApprovalNode{TState}"/>, this node does not throw on rejection but stores the outcome.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class ConditionalApprovalNode<TState> : GraphNode<TState> where TState : class
{
    private readonly IApprovalHandler<TState> _approvalHandler;
    private readonly string? _approvalMessage;
    private readonly TimeSpan? _timeout;
    private readonly string _outcomePropertyName;
    private readonly ILogger<ConditionalApprovalNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalApprovalNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the approval node.</param>
    /// <param name="approvalHandler">The approval handler to use.</param>
    /// <param name="approvalMessage">Optional message to display when requesting approval.</param>
    /// <param name="outcomePropertyName">The name of the property to store the approval outcome. Default is "ApprovalOutcome".</param>
    /// <param name="timeout">Optional timeout for approval. If null, waits indefinitely.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public ConditionalApprovalNode(
        string name,
        IApprovalHandler<TState> approvalHandler,
        string? approvalMessage = null,
        string outcomePropertyName = "ApprovalOutcome",
        TimeSpan? timeout = null,
        ILogger<ConditionalApprovalNode<TState>>? logger = null)
        : base(name, CreateHandler(
            approvalHandler ?? throw new ArgumentNullException(nameof(approvalHandler)),
            approvalMessage,
            outcomePropertyName ?? throw new ArgumentNullException(nameof(outcomePropertyName)),
            timeout,
            logger,
            name))
    {
        _approvalHandler = approvalHandler;
        _approvalMessage = approvalMessage;
        _outcomePropertyName = outcomePropertyName;
        _timeout = timeout;
        _logger = logger;
        Description = $"Requires conditional approval: {approvalMessage ?? "Approval required"}";
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        IApprovalHandler<TState> approvalHandler,
        string? approvalMessage,
        string outcomePropertyName,
        TimeSpan? timeout,
        ILogger<ConditionalApprovalNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            var workflowRunId = GetWorkflowRunId(state) ?? Guid.NewGuid().ToString("N");

            logger?.LogInformation(
                "Node {NodeName}: Requesting conditional approval for workflow '{WorkflowRunId}'. Message: {Message}",
                nodeName,
                workflowRunId,
                approvalMessage ?? "No message provided");

            // Set initial outcome to Pending
            SetApprovalOutcome(state, outcomePropertyName, ApprovalOutcome.Pending);

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

                    var startTime = DateTimeOffset.UtcNow;
                    while (DateTimeOffset.UtcNow - startTime < timeout.Value)
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
                        // Check final status
                        approved = await approvalHandler.IsApprovedAsync(workflowRunId, nodeName, ct).ConfigureAwait(false);
                        if (!approved)
                        {
                            SetApprovalOutcome(state, outcomePropertyName, ApprovalOutcome.Rejected);
                            logger?.LogWarning(
                                "Node {NodeName}: Approval timeout for workflow '{WorkflowRunId}'.",
                                nodeName,
                                workflowRunId);
                            return state; // Return state with Rejected outcome, don't throw
                        }
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

            // Check final approval status
            approved = await approvalHandler.IsApprovedAsync(workflowRunId, nodeName, ct).ConfigureAwait(false);

            if (approved)
            {
                SetApprovalOutcome(state, outcomePropertyName, ApprovalOutcome.Approved);
                logger?.LogInformation(
                    "Node {NodeName}: Approval granted for workflow '{WorkflowRunId}'.",
                    nodeName,
                    workflowRunId);
            }
            else
            {
                SetApprovalOutcome(state, outcomePropertyName, ApprovalOutcome.Rejected);
                logger?.LogInformation(
                    "Node {NodeName}: Approval rejected for workflow '{WorkflowRunId}'.",
                    nodeName,
                    workflowRunId);
            }

            return state;
        };
    }

    private static string? GetWorkflowRunId(TState state)
    {
        var type = typeof(TState);
        var prop = type.GetProperty("WorkflowRunId") ?? type.GetProperty("RunId");
        return prop?.GetValue(state)?.ToString();
    }

    private static void SetApprovalOutcome(TState state, string propertyName, ApprovalOutcome outcome)
    {
        var type = typeof(TState);
        var prop = type.GetProperty(propertyName);
        if (prop != null && prop.CanWrite)
        {
            try
            {
                // Try to set as ApprovalOutcome enum
                if (prop.PropertyType == typeof(ApprovalOutcome) || prop.PropertyType == typeof(ApprovalOutcome?))
                {
                    prop.SetValue(state, outcome);
                }
                // Try to set as string
                else if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(state, outcome.ToString());
                }
                // Try to set as bool (Approved = true, others = false)
                else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                {
                    prop.SetValue(state, outcome == ApprovalOutcome.Approved);
                }
            }
            catch
            {
                // Ignore if we can't set the property
            }
        }
    }
}
