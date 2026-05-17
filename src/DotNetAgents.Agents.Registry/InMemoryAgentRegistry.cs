using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.Registry;

/// <summary>
/// In-memory implementation of <see cref="IAgentRegistry"/>.
/// Suitable for single-instance deployments.
/// </summary>
public class InMemoryAgentRegistry : IAgentRegistry
{
    private readonly Dictionary<string, AgentInfo> _agents = new();
    private readonly ILogger<InMemoryAgentRegistry>? _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAgentRegistry"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public InMemoryAgentRegistry(ILogger<InMemoryAgentRegistry>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task RegisterAsync(
        AgentCapabilities capabilities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var agentInfo = new AgentInfo
            {
                AgentId = capabilities.AgentId,
                AgentType = capabilities.AgentType,
                Status = AgentStatus.Available,
                Capabilities = capabilities,
                LastHeartbeat = DateTimeOffset.UtcNow,
                CurrentTaskCount = 0
            };

            _agents[capabilities.AgentId] = agentInfo;

            _logger?.LogInformation(
                "Registered agent {AgentId} of type {AgentType} with {ToolCount} tools and {IntentCount} intents",
                capabilities.AgentId,
                capabilities.AgentType,
                capabilities.SupportedTools.Length,
                capabilities.SupportedIntents.Length);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_agents.Remove(agentId))
            {
                _logger?.LogInformation("Unregistered agent {AgentId}", agentId);
            }
            else
            {
                _logger?.LogWarning("Attempted to unregister unknown agent {AgentId}", agentId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateStatusAsync(
        string agentId,
        AgentStatus status,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_agents.TryGetValue(agentId, out var agentInfo))
            {
                _agents[agentId] = agentInfo with { Status = status };
                _logger?.LogDebug(
                    "Updated agent {AgentId} status to {Status}",
                    agentId,
                    status);
            }
            else
            {
                _logger?.LogWarning("Attempted to update status for unknown agent {AgentId}", agentId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateTaskCountAsync(
        string agentId,
        int taskCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        cancellationToken.ThrowIfCancellationRequested();

        if (taskCount < 0)
            throw new ArgumentException("Task count cannot be negative.", nameof(taskCount));

        lock (_lock)
        {
            if (_agents.TryGetValue(agentId, out var agentInfo))
            {
                _agents[agentId] = agentInfo with { CurrentTaskCount = taskCount };
                _logger?.LogDebug(
                    "Updated agent {AgentId} task count to {TaskCount}",
                    agentId,
                    taskCount);
            }
            else
            {
                _logger?.LogWarning("Attempted to update task count for unknown agent {AgentId}", agentId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentInfo>> FindByCapabilityAsync(
        string capability,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(capability);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var matchingAgents = _agents.Values
                .Where(agent => agent.Capabilities.SupportedTools.Contains(capability, StringComparer.OrdinalIgnoreCase) ||
                               agent.Capabilities.SupportedIntents.Contains(capability, StringComparer.OrdinalIgnoreCase))
                .ToList();

            _logger?.LogDebug(
                "Found {Count} agents with capability {Capability}",
                matchingAgents.Count,
                capability);

            return Task.FromResult<IReadOnlyList<AgentInfo>>(matchingAgents);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentInfo>> FindByTypeAsync(
        string agentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentType);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var matchingAgents = _agents.Values
                .Where(agent => string.Equals(agent.AgentType, agentType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger?.LogDebug(
                "Found {Count} agents of type {AgentType}",
                matchingAgents.Count,
                agentType);

            return Task.FromResult<IReadOnlyList<AgentInfo>>(matchingAgents);
        }
    }

    /// <inheritdoc />
    public Task<AgentInfo?> GetByIdAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _agents.TryGetValue(agentId, out var agentInfo);
            return Task.FromResult(agentInfo);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentInfo>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<AgentInfo>>(_agents.Values.ToList());
        }
    }

    /// <inheritdoc />
    public Task RecordHeartbeatAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_agents.TryGetValue(agentId, out var agentInfo))
            {
                _agents[agentId] = agentInfo with { LastHeartbeat = DateTimeOffset.UtcNow };
                _logger?.LogTrace("Recorded heartbeat for agent {AgentId}", agentId);
            }
            else
            {
                _logger?.LogWarning("Attempted to record heartbeat for unknown agent {AgentId}", agentId);
            }
        }

        return Task.CompletedTask;
    }
}
