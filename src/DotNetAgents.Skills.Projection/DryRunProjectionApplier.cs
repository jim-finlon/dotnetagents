using System.Collections.Concurrent;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Story aff01407 (SUC Projection Framework P1). Dry-run <see cref="IProjectionApplier"/>:
/// records each apply call into <see cref="Captured"/> without writing to disk. Used by
/// tests + the SUC-17 panel-load metric path (story 9bd8f796 Slice B) that needs to count
/// "would-be vendor projections" per skill without producing side effects.
/// </summary>
/// <remarks>
/// Thread-safe — <see cref="Captured"/> is backed by a <see cref="ConcurrentQueue{T}"/> so
/// multiple projectors can be applied in parallel against the same applier instance.
/// </remarks>
public sealed class DryRunProjectionApplier : IProjectionApplier
{
    private readonly ConcurrentQueue<SkillProjectionResult> _captured = new();

    /// <summary>
    /// All apply calls captured by this instance, in submission order. Each entry's
    /// <see cref="SkillProjectionResult.Outcome"/> is <see cref="ProjectionOutcome.DryRun"/>.
    /// </summary>
    public IReadOnlyCollection<SkillProjectionResult> Captured => _captured.ToArray();

    public Task<SkillProjectionResult> ApplyAsync(
        SkillProjection projection,
        string baseDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var targetFullPath = Path.Combine(baseDirectory, projection.TargetPath, projection.FileName);
        var result = new SkillProjectionResult(projection, targetFullPath, ProjectionOutcome.DryRun);
        _captured.Enqueue(result);
        return Task.FromResult(result);
    }

    /// <summary>Reset the captured-apply log so this applier can be reused across cases.</summary>
    public void Clear()
    {
        while (_captured.TryDequeue(out _)) { /* drain */ }
    }
}
