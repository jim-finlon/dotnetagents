// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Skills;

/// <summary>
/// Immutable install receipt emitted when an actor receives a projected skill artifact.
/// </summary>
public sealed record SkillInstallReceipt(
    string SchemaVersion,
    string ActorId,
    string SkillId,
    string Version,
    DateTimeOffset InstalledAtUtc,
    string ProjectionKind,
    IReadOnlyList<string> ProjectorWarnings,
    string Checksum);
