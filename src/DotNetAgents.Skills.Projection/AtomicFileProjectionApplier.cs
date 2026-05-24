// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Story aff01407 (SUC Projection Framework P1) and e399bfce (P1 follow-up). Production
/// <see cref="IProjectionApplier"/>: writes <see cref="SkillProjectionMode.Write"/> and
/// <see cref="SkillProjectionMode.Sidecar"/> projections to disk through
/// <see cref="AtomicFileWriter"/>, and grafts <see cref="SkillProjectionMode.AppendSection"/>
/// projections through <see cref="MarkerBoundedSectionWriter"/> using
/// <see cref="SkillProjection.MarkerKey"/> as the per-section identifier.
/// </summary>
public sealed class AtomicFileProjectionApplier : IProjectionApplier
{
    public async Task<SkillProjectionResult> ApplyAsync(
        SkillProjection projection,
        string baseDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var targetFullPath = Path.Combine(baseDirectory, projection.TargetPath, projection.FileName);

        AtomicWriteOutcome outcome;
        if (projection.Mode == SkillProjectionMode.AppendSection)
        {
            if (string.IsNullOrWhiteSpace(projection.MarkerKey))
            {
                throw new InvalidOperationException(
                    "AppendSection projections must declare a non-empty MarkerKey "
                    + "(see SkillProjection.MarkerKey doc — typically the skill id). "
                    + "Projector emitting AppendSection without a MarkerKey: review the projector's Project() call site.");
            }

            outcome = await MarkerBoundedSectionWriter.WriteAsync(
                targetFullPath, projection.MarkerKey, projection.Contents, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            outcome = await AtomicFileWriter.WriteAsync(targetFullPath, projection.Contents, cancellationToken)
                .ConfigureAwait(false);
        }

        return new SkillProjectionResult(
            projection,
            targetFullPath,
            outcome == AtomicWriteOutcome.Written ? ProjectionOutcome.Written : ProjectionOutcome.NoChange);
    }
}
