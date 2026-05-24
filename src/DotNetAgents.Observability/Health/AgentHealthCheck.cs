// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotNetAgents.Observability.Health;

/// <summary>
/// Health check for DotNetAgents components.
/// </summary>
public class AgentHealthCheck : IHealthCheck
{
    private readonly IReadOnlyList<IComponentHealthCheck> _componentChecks;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentHealthCheck"/> class.
    /// </summary>
    /// <param name="componentChecks">Optional list of component-specific health checks.</param>
    public AgentHealthCheck(IEnumerable<IComponentHealthCheck>? componentChecks = null)
    {
        _componentChecks = componentChecks?.ToList().AsReadOnly() ?? new List<IComponentHealthCheck>().AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_componentChecks.Count == 0)
        {
            return HealthCheckResult.Healthy("No component checks configured.");
        }

        var results = new List<(string Component, HealthCheckResult Result)>();
        var allHealthy = true;

        foreach (var check in _componentChecks)
        {
            try
            {
                var result = await check.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
                results.Add((check.ComponentName, result));

                if (result.Status != HealthStatus.Healthy)
                {
                    allHealthy = false;
                }
            }
            catch (Exception ex)
            {
                results.Add((check.ComponentName, HealthCheckResult.Unhealthy("Health check failed", ex)));
                allHealthy = false;
            }
        }

        var data = results.ToDictionary(
            r => r.Component,
            r => (object)new
            {
                Status = r.Result.Status.ToString(),
                Description = r.Result.Description,
                Exception = r.Result.Exception?.Message
            });

        if (allHealthy)
        {
            return HealthCheckResult.Healthy("All components are healthy", data);
        }

        var unhealthyComponents = results
            .Where(r => r.Result.Status != HealthStatus.Healthy)
            .Select(r => r.Component)
            .ToList();

        return HealthCheckResult.Degraded(
            $"Some components are unhealthy: {string.Join(", ", unhealthyComponents)}",
            data: data);
    }
}

/// <summary>
/// Interface for component-specific health checks.
/// </summary>
public interface IComponentHealthCheck
{
    /// <summary>
    /// Gets the name of the component.
    /// </summary>
    string ComponentName { get; }

    /// <summary>
    /// Performs a health check for the component.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A health check result.</returns>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
