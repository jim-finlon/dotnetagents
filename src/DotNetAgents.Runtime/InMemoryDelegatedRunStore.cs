// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace DotNetAgents.Runtime;

public sealed class InMemoryDelegatedRunStore : IDelegatedRunStore
{
    private readonly ConcurrentDictionary<string, DelegatedAgentRun> _runs = new(StringComparer.Ordinal);

    public Task<DelegatedAgentRun> CreateAsync(
        DelegatedAgentRun run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (!_runs.TryAdd(run.Id, run))
            throw new InvalidOperationException($"Delegated run '{run.Id}' already exists.");
        return Task.FromResult(run);
    }

    public Task<DelegatedAgentRun?> GetAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        _runs.TryGetValue(runId, out var run);
        return Task.FromResult(run);
    }

    public Task<DelegatedAgentRun> UpdateAsync(
        DelegatedAgentRun run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        _runs[run.Id] = run;
        return Task.FromResult(run);
    }
}
