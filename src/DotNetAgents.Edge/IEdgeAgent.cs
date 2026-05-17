using DotNetAgents.Abstractions.Models;

namespace DotNetAgents.Edge;

/// <summary>
/// Edge-optimized agent interface for mobile and edge deployments.
/// </summary>
public interface IEdgeAgent
{
    /// <summary>
    /// Gets whether the agent is currently online.
    /// </summary>
    bool IsOnline { get; }

    /// <summary>
    /// Gets the offline mode status.
    /// </summary>
    OfflineModeStatus OfflineMode { get; }

    /// <summary>
    /// Executes a task with automatic offline fallback.
    /// </summary>
    /// <param name="input">The input to process.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The execution result.</returns>
    Task<EdgeExecutionResult> ExecuteAsync(
        string input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a task in offline mode only.
    /// </summary>
    /// <param name="input">The input to process.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The execution result.</returns>
    Task<EdgeExecutionResult> ExecuteOfflineAsync(
        string input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes offline cache with online services.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SyncAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Status of offline mode.
/// </summary>
public enum OfflineModeStatus
{
    /// <summary>
    /// Online mode - all features available.
    /// </summary>
    Online,

    /// <summary>
    /// Offline mode - using cached/local resources only.
    /// </summary>
    Offline,

    /// <summary>
    /// Degraded mode - some features unavailable.
    /// </summary>
    Degraded
}

/// <summary>
/// Result of edge agent execution.
/// </summary>
public class EdgeExecutionResult
{
    /// <summary>
    /// Gets or sets the execution output.
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the execution was performed offline.
    /// </summary>
    public bool WasOffline { get; set; }

    /// <summary>
    /// Gets or sets the execution mode used.
    /// </summary>
    public OfflineModeStatus Mode { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1).
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets any warnings or limitations.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
