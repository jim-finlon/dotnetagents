using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// SignalR-based approval handler for web applications.
/// Note: This requires Microsoft.AspNetCore.SignalR package to be added by the application.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class SignalRApprovalHandler<TState> : IApprovalHandler<TState> where TState : class
{
    private readonly IApprovalStore<TState> _approvalStore;
    private readonly ILogger<SignalRApprovalHandler<TState>>? _logger;
    private readonly object? _hubContext; // Using object? to avoid SignalR dependency in library

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRApprovalHandler{TState}"/> class.
    /// </summary>
    /// <param name="approvalStore">The approval store for persisting approval requests.</param>
    /// <param name="hubContext">Optional SignalR hub context (requires Microsoft.AspNetCore.SignalR package).</param>
    /// <param name="logger">Optional logger for approval operations.</param>
    public SignalRApprovalHandler(
        IApprovalStore<TState> approvalStore,
        object? hubContext = null,
        ILogger<SignalRApprovalHandler<TState>>? logger = null)
    {
        _approvalStore = approvalStore ?? throw new ArgumentNullException(nameof(approvalStore));
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> RequestApprovalAsync(
        string workflowRunId,
        string nodeName,
        TState state,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        ArgumentNullException.ThrowIfNull(state);

        cancellationToken.ThrowIfCancellationRequested();

        // Store approval request
        var request = new ApprovalRequest<TState>
        {
            WorkflowRunId = workflowRunId,
            NodeName = nodeName,
            State = state,
            Message = message,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = ApprovalStatus.Pending
        };

        await _approvalStore.SaveAsync(request, cancellationToken).ConfigureAwait(false);

        // Send SignalR notification if hub context is available
        if (_hubContext != null)
        {
            // Use reflection to call SignalR methods to avoid dependency
            try
            {
                var sendAsyncMethod = _hubContext.GetType().GetMethod("SendAsync");
                if (sendAsyncMethod != null)
                {
                    var clientsProperty = _hubContext.GetType().GetProperty("Clients");
                    if (clientsProperty != null)
                    {
                        var clients = clientsProperty.GetValue(_hubContext);
                        var allProperty = clients?.GetType().GetProperty("All");
                        var all = allProperty?.GetValue(clients);
                        var sendAsyncOnAll = all?.GetType().GetMethod("SendAsync");
                        sendAsyncOnAll?.Invoke(all, new object[] { "ApprovalRequested", workflowRunId, nodeName, message, cancellationToken });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to send SignalR notification for approval request");
            }
        }

        _logger?.LogInformation(
            "Approval requested. Workflow: {WorkflowRunId}, Node: {NodeName}, Message: {Message}",
            workflowRunId,
            nodeName,
            message ?? "No message");

        return false; // Always pending for SignalR handler
    }

    /// <inheritdoc/>
    public async Task<bool> IsApprovedAsync(
        string workflowRunId,
        string nodeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);

        cancellationToken.ThrowIfCancellationRequested();

        var request = await _approvalStore.GetAsync(workflowRunId, nodeName, cancellationToken).ConfigureAwait(false);

        if (request == null)
            return false;

        return request.Status == ApprovalStatus.Approved;
    }
}

/// <summary>
/// SignalR hub for approval notifications.
/// Note: This requires Microsoft.AspNetCore.SignalR package to be added by the application.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class ApprovalHub<TState> where TState : class
{
    private readonly IApprovalStore<TState> _approvalStore;
    private readonly ILogger<ApprovalHub<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalHub{TState}"/> class.
    /// </summary>
    /// <param name="approvalStore">The approval store.</param>
    /// <param name="logger">Optional logger.</param>
    public ApprovalHub(
        IApprovalStore<TState> approvalStore,
        ILogger<ApprovalHub<TState>>? logger = null)
    {
        _approvalStore = approvalStore ?? throw new ArgumentNullException(nameof(approvalStore));
        _logger = logger;
    }

    /// <summary>
    /// Approves or rejects a pending approval request.
    /// </summary>
    /// <param name="workflowRunId">The workflow run ID.</param>
    /// <param name="nodeName">The node name.</param>
    /// <param name="approved">True to approve; false to reject.</param>
    public async Task ApproveAsync(string workflowRunId, string nodeName, bool approved)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);

        var request = await _approvalStore.GetAsync(workflowRunId, nodeName).ConfigureAwait(false);

        if (request == null)
        {
            _logger?.LogWarning(
                "Approval request not found. Workflow: {WorkflowRunId}, Node: {NodeName}",
                workflowRunId,
                nodeName);
            return;
        }

        var updatedRequest = request with
        {
            Status = approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected,
            RespondedAt = DateTimeOffset.UtcNow
        };

        await _approvalStore.SaveAsync(updatedRequest).ConfigureAwait(false);

        _logger?.LogInformation(
            "Approval {Status}. Workflow: {WorkflowRunId}, Node: {NodeName}",
            approved ? "granted" : "rejected",
            workflowRunId,
            nodeName);
    }
}

/// <summary>
/// Interface for storing approval requests.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public interface IApprovalStore<TState> where TState : class
{
    /// <summary>
    /// Saves an approval request.
    /// </summary>
    Task SaveAsync(ApprovalRequest<TState> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an approval request.
    /// </summary>
    Task<ApprovalRequest<TState>?> GetAsync(
        string workflowRunId,
        string nodeName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Approval request with status tracking.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public record ApprovalRequest<TState> where TState : class
{
    /// <summary>
    /// Gets the workflow run ID.
    /// </summary>
    public string WorkflowRunId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the node name requesting approval.
    /// </summary>
    public string NodeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the workflow state at the time of the request.
    /// </summary>
    public TState State { get; init; } = null!;

    /// <summary>
    /// Gets the optional message describing what needs approval.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the timestamp when the approval was requested.
    /// </summary>
    public DateTimeOffset RequestedAt { get; init; }

    /// <summary>
    /// Gets the approval status.
    /// </summary>
    public ApprovalStatus Status { get; init; } = ApprovalStatus.Pending;

    /// <summary>
    /// Gets the timestamp when the approval was responded to.
    /// </summary>
    public DateTimeOffset? RespondedAt { get; init; }
}

/// <summary>
/// Approval status enumeration.
/// </summary>
public enum ApprovalStatus
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
    Rejected
}
