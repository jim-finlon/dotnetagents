using DotNetAgents.Abstractions.Models;
using DotNetAgents.Core.Agents;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Edge;

/// <summary>
/// Edge-optimized agent implementation with offline support.
/// </summary>
public class EdgeAgent : IEdgeAgent
{
    private readonly AgentExecutor _agentExecutor;
    private readonly IOfflineCache _cache;
    private readonly IEdgeModelProvider? _edgeModelProvider;
    private readonly ILogger<EdgeAgent>? _logger;
    private readonly INetworkMonitor? _networkMonitor;
    private OfflineModeStatus _offlineMode = OfflineModeStatus.Online;

    /// <summary>
    /// Initializes a new instance of the <see cref="EdgeAgent"/> class.
    /// </summary>
    /// <param name="agentExecutor">The underlying agent executor.</param>
    /// <param name="cache">The offline cache.</param>
    /// <param name="edgeModelProvider">Optional edge model provider for offline execution.</param>
    /// <param name="networkMonitor">Optional network monitor.</param>
    /// <param name="logger">Optional logger instance.</param>
    public EdgeAgent(
        AgentExecutor agentExecutor,
        IOfflineCache cache,
        IEdgeModelProvider? edgeModelProvider = null,
        INetworkMonitor? networkMonitor = null,
        ILogger<EdgeAgent>? logger = null)
    {
        _agentExecutor = agentExecutor ?? throw new ArgumentNullException(nameof(agentExecutor));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _edgeModelProvider = edgeModelProvider;
        _networkMonitor = networkMonitor;
        _logger = logger;

        // Monitor network status
        if (_networkMonitor != null)
        {
            _networkMonitor.StatusChanged += OnNetworkStatusChanged;
            _offlineMode = _networkMonitor.IsOnline ? OfflineModeStatus.Online : OfflineModeStatus.Offline;
        }
    }

    /// <inheritdoc />
    public bool IsOnline => _networkMonitor?.IsOnline ?? true;

    /// <inheritdoc />
    public OfflineModeStatus OfflineMode => _offlineMode;

    /// <inheritdoc />
    public async Task<EdgeExecutionResult> ExecuteAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        cancellationToken.ThrowIfCancellationRequested();

        // Check cache first
        var cacheKey = GenerateCacheKey(input);
        var cachedResult = await _cache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (cachedResult != null)
        {
            _logger?.LogDebug("Returning cached result for input");
            return new EdgeExecutionResult
            {
                Output = cachedResult,
                WasOffline = true,
                Mode = OfflineModeStatus.Offline,
                ConfidenceScore = 0.9 // Cached results have high confidence
            };
        }

        // Try online execution if available
        if (IsOnline && _offlineMode != OfflineModeStatus.Offline)
        {
            try
            {
                var result = await _agentExecutor.InvokeAsync(input, options: null, cancellationToken).ConfigureAwait(false);

                // Cache the result
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(24), cancellationToken)
                    .ConfigureAwait(false);

                return new EdgeExecutionResult
                {
                    Output = result,
                    WasOffline = false,
                    Mode = OfflineModeStatus.Online,
                    ConfidenceScore = 1.0
                };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                _logger?.LogWarning(ex, "Online execution failed, falling back to offline mode");
                _offlineMode = OfflineModeStatus.Degraded;
            }
        }

        // Fall back to offline execution
        return await ExecuteOfflineAsync(input, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<EdgeExecutionResult> ExecuteOfflineAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        cancellationToken.ThrowIfCancellationRequested();

        var warnings = new List<string>();

        // Try edge model provider if available
        if (_edgeModelProvider != null)
        {
            try
            {
                var result = await _edgeModelProvider.GenerateAsync(input, cancellationToken)
                    .ConfigureAwait(false);

                return new EdgeExecutionResult
                {
                    Output = result,
                    WasOffline = true,
                    Mode = OfflineModeStatus.Offline,
                    ConfidenceScore = 0.7, // Edge models may have lower accuracy
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Edge model provider failed");
                warnings.Add("Edge model unavailable");
            }
        }
        else
        {
            warnings.Add("No edge model provider configured");
        }

        // Last resort: return a basic response
        return new EdgeExecutionResult
        {
            Output = "I'm currently offline and cannot process this request. Please try again when online.",
            WasOffline = true,
            Mode = OfflineModeStatus.Offline,
            ConfidenceScore = 0.0,
            Warnings = warnings
        };
    }

    /// <inheritdoc />
    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsOnline)
        {
            _logger?.LogWarning("Cannot sync while offline");
            return;
        }

        // Sync cache with online services
        // This is a placeholder - in production, would sync with backend
        _logger?.LogInformation("Syncing offline cache with online services");
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static string GenerateCacheKey(string input)
    {
        // Simple hash-based cache key
        var hash = input.GetHashCode();
        return $"edge_cache_{hash}";
    }

    private void OnNetworkStatusChanged(object? sender, NetworkStatusEventArgs e)
    {
        _offlineMode = e.IsOnline ? OfflineModeStatus.Online : OfflineModeStatus.Offline;
        _logger?.LogInformation("Network status changed: {Status}", _offlineMode);
    }
}

/// <summary>
/// Edge model provider for offline execution.
/// </summary>
public interface IEdgeModelProvider
{
    /// <summary>
    /// Generates a response using an edge-optimized model.
    /// </summary>
    /// <param name="input">The input to process.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The generated output.</returns>
    Task<string> GenerateAsync(string input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Network monitor for detecting online/offline status.
/// </summary>
public interface INetworkMonitor
{
    /// <summary>
    /// Gets whether the network is currently online.
    /// </summary>
    bool IsOnline { get; }

    /// <summary>
    /// Event raised when network status changes.
    /// </summary>
    event EventHandler<NetworkStatusEventArgs>? StatusChanged;
}

/// <summary>
/// Network status event arguments.
/// </summary>
public class NetworkStatusEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets whether the network is online.
    /// </summary>
    public bool IsOnline { get; set; }
}
