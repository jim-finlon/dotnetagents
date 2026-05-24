// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Proactive;

public sealed record ProactiveSignal(string TriggerName, string JobName, IReadOnlyDictionary<string, string>? Metadata = null);
public sealed record ProactiveExecutionContext(string TriggerName, IReadOnlyDictionary<string, string>? Metadata = null);

public interface IProactiveTrigger
{
    string Name { get; }
    IAsyncEnumerable<ProactiveSignal> ListenAsync(CancellationToken cancellationToken = default);
}

public interface IProactiveJob
{
    string Name { get; }
    Task ExecuteAsync(ProactiveExecutionContext context, CancellationToken cancellationToken = default);
}

public interface IProactiveJobDispatcher
{
    Task DispatchAsync(ProactiveSignal signal, CancellationToken cancellationToken = default);
}

public sealed class InMemoryProactiveJobDispatcher(
    IEnumerable<IProactiveJob> jobs,
    ILogger<InMemoryProactiveJobDispatcher> logger) : IProactiveJobDispatcher
{
    private readonly IReadOnlyDictionary<string, IProactiveJob> _jobs =
        jobs.ToDictionary(j => j.Name, StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<InMemoryProactiveJobDispatcher> _logger = logger;

    public Task DispatchAsync(ProactiveSignal signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);
        if (_jobs.TryGetValue(signal.JobName, out var job))
        {
            var context = new ProactiveExecutionContext(signal.TriggerName, signal.Metadata);
            return job.ExecuteAsync(context, cancellationToken);
        }

        _logger.LogWarning("No proactive job found for '{JobName}'", signal.JobName);
        return Task.CompletedTask;
    }
}

public sealed class ProactiveRuntime(
    IEnumerable<IProactiveTrigger> triggers,
    IProactiveJobDispatcher dispatcher)
{
    private readonly IReadOnlyList<IProactiveTrigger> _triggers = triggers.ToList();
    private readonly IProactiveJobDispatcher _dispatcher = dispatcher;

    public async Task PollOnceAsync(CancellationToken cancellationToken = default)
    {
        foreach (var trigger in _triggers)
        {
            await foreach (var signal in trigger.ListenAsync(cancellationToken).ConfigureAwait(false))
            {
                await _dispatcher.DispatchAsync(signal, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetAgentsProactive(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IProactiveJobDispatcher, InMemoryProactiveJobDispatcher>();
        services.AddSingleton<ProactiveRuntime>();
        return services;
    }
}
