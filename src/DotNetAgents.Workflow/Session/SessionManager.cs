using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Workflow.Session.Bootstrap;
using DotNetAgents.Workflow.Session.Storage;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Session;

/// <summary>
/// Default implementation of <see cref="ISessionManager"/>.
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly ISnapshotStore _snapshotStore;
    private readonly IMilestoneStore _milestoneStore;
    private readonly ISessionContextStore _contextStore;
    private readonly IBootstrapGenerator _bootstrapGenerator;
    private readonly ILogger<SessionManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionManager"/> class.
    /// </summary>
    /// <param name="snapshotStore">The snapshot store.</param>
    /// <param name="milestoneStore">The milestone store.</param>
    /// <param name="contextStore">The session context store.</param>
    /// <param name="bootstrapGenerator">The bootstrap generator.</param>
    /// <param name="logger">The logger.</param>
    public SessionManager(
        ISnapshotStore snapshotStore,
        IMilestoneStore milestoneStore,
        ISessionContextStore contextStore,
        IBootstrapGenerator bootstrapGenerator,
        ILogger<SessionManager> logger)
    {
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _milestoneStore = milestoneStore ?? throw new ArgumentNullException(nameof(milestoneStore));
        _contextStore = contextStore ?? throw new ArgumentNullException(nameof(contextStore));
        _bootstrapGenerator = bootstrapGenerator ?? throw new ArgumentNullException(nameof(bootstrapGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<WorkflowSnapshot> CreateSnapshotAsync(WorkflowSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        try
        {
            _logger.LogDebug("Creating snapshot for session {SessionId}", snapshot.SessionId);

            var created = await _snapshotStore.CreateAsync(snapshot, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Snapshot created. SnapshotId: {SnapshotId}, SessionId: {SessionId}, SnapshotNumber: {SnapshotNumber}",
                created.Id,
                created.SessionId,
                created.SnapshotNumber);

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create snapshot for session {SessionId}", snapshot.SessionId);
            throw new AgentException(
                $"Failed to create snapshot: {ex.Message}",
                ErrorCategory.WorkflowError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<WorkflowSnapshot?> GetSnapshotAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _snapshotStore.GetByIdAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get snapshot {SnapshotId}", snapshotId);
            throw new AgentException(
                $"Failed to get snapshot: {ex.Message}",
                ErrorCategory.WorkflowError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkflowSnapshot>> GetSnapshotsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        try
        {
            return await _snapshotStore.GetBySessionIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get snapshots for session {SessionId}", sessionId);
            throw new AgentException(
                $"Failed to get snapshots: {ex.Message}",
                ErrorCategory.WorkflowError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<WorkflowSnapshot?> GetLatestSnapshotAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        try
        {
            return await _snapshotStore.GetLatestAsync(sessionId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest snapshot for session {SessionId}", sessionId);
            throw new AgentException(
                $"Failed to get latest snapshot: {ex.Message}",
                ErrorCategory.WorkflowError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Milestone> CreateMilestoneAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        if (milestone == null)
            throw new ArgumentNullException(nameof(milestone));

        try
        {
            _logger.LogDebug("Creating milestone for session {SessionId}", milestone.SessionId);

            var created = await _milestoneStore.CreateAsync(milestone, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Milestone created. MilestoneId: {MilestoneId}, SessionId: {SessionId}, Name: {Name}",
                created.Id,
                created.SessionId,
                created.Name);

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create milestone for session {SessionId}", milestone.SessionId);
            throw new AgentException(
                $"Failed to create milestone: {ex.Message}",
                ErrorCategory.WorkflowError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Milestone> UpdateMilestoneAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        if (milestone == null)
            throw new ArgumentNullException(nameof(milestone));

        try
        {
            _logger.LogDebug("Updating milestone {MilestoneId}", milestone.Id);

            var updated = await _milestoneStore.UpdateAsync(milestone, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Milestone updated. MilestoneId: {MilestoneId}, Status: {Status}", updated.Id, updated.Status);

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update milestone {MilestoneId}", milestone.Id);
            throw new AgentException(
                $"Failed to update milestone: {ex.Message}",
                ErrorCategory.WorkflowError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Milestone>> GetMilestonesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        try
        {
            return await _milestoneStore.GetBySessionIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get milestones for session {SessionId}", sessionId);
            throw new AgentException(
                $"Failed to get milestones: {ex.Message}",
                ErrorCategory.WorkflowError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<SessionContext> CreateOrUpdateContextAsync(SessionContext context, CancellationToken cancellationToken = default)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        try
        {
            _logger.LogDebug("Creating or updating context for session {SessionId}", context.SessionId);

            var updated = await _contextStore.CreateOrUpdateAsync(context, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Context created or updated for session {SessionId}", updated.SessionId);

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create or update context for session {SessionId}", context.SessionId);
            throw new AgentException(
                $"Failed to create or update context: {ex.Message}",
                ErrorCategory.WorkflowError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<SessionContext?> GetContextAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        try
        {
            return await _contextStore.GetBySessionIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get context for session {SessionId}", sessionId);
            throw new AgentException(
                $"Failed to get context: {ex.Message}",
                ErrorCategory.WorkflowError,
                ex);
        }
    }

    /// <summary>
    /// Generates a bootstrap payload for session resumption.
    /// </summary>
    /// <param name="data">The bootstrap data.</param>
    /// <param name="format">The output format.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The generated bootstrap payload.</returns>
    public async Task<BootstrapPayload> GenerateBootstrapAsync(
        BootstrapData data,
        BootstrapFormat format = BootstrapFormat.Json,
        CancellationToken cancellationToken = default)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            _logger.LogDebug("Generating bootstrap for session {SessionId}", data.SessionId);

            var payload = await _bootstrapGenerator.GenerateAsync(data, format, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Bootstrap generated. SessionId: {SessionId}, Format: {Format}, Size: {Size} bytes",
                data.SessionId,
                format,
                payload.FormattedContent.Length);

            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate bootstrap for session {SessionId}", data.SessionId);
            throw new AgentException(
                $"Failed to generate bootstrap: {ex.Message}",
                ErrorCategory.WorkflowError,
                ex);
        }
    }
}
