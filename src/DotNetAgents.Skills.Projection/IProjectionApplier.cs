// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Story aff01407 (SUC Projection Framework P1). Applies a rendered <see cref="SkillProjection"/>
/// to its target — the production binding writes to disk via <see cref="AtomicFileWriter"/>;
/// the dry-run binding records the would-be apply without touching disk (used by tests + the
/// SUC-17 panel-load metric path that needs vendor-projection counts without side effects).
/// </summary>
public interface IProjectionApplier
{
    /// <summary>
    /// Apply <paramref name="projection"/> rooted at <paramref name="baseDirectory"/>
    /// (typically the repo root or actor home, per the projection's originating context).
    /// </summary>
    Task<SkillProjectionResult> ApplyAsync(
        SkillProjection projection,
        string baseDirectory,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of an <see cref="IProjectionApplier.ApplyAsync"/> call. Distinguishes a real disk
/// write (<see cref="Outcome"/>=<see cref="ProjectionOutcome.Written"/>), a no-op idempotent
/// hit (<see cref="ProjectionOutcome.NoChange"/>), and a dry-run capture
/// (<see cref="ProjectionOutcome.DryRun"/>).
/// </summary>
/// <param name="Projection">The projection that was applied (or dry-run captured).</param>
/// <param name="TargetFullPath">Absolute filesystem path the projection resolved to (or would have).</param>
/// <param name="Outcome">What the applier did.</param>
public sealed record SkillProjectionResult(
    SkillProjection Projection,
    string TargetFullPath,
    ProjectionOutcome Outcome);

/// <summary>Outcome categories returned by <see cref="IProjectionApplier"/>.</summary>
public enum ProjectionOutcome
{
    /// <summary>Disk was written (new file or content-changed overwrite).</summary>
    Written,

    /// <summary>Destination already matched byte-for-byte; nothing was touched.</summary>
    NoChange,

    /// <summary>Dry-run applier: captured the would-be apply without writing.</summary>
    DryRun,
}
