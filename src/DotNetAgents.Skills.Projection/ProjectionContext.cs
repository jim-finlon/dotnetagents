// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Per-projection context. Carries actor identity, target scope (repo vs user), and behavior
/// flags that all projectors consume uniformly.
/// </summary>
/// <param name="ActorId">
/// Lower-case <c>{machine}-{llm}</c> actor identity from CredentialsAgent
/// (e.g. <c>agent-alpha</c>). Used for install-receipt scoping.
/// </param>
/// <param name="ActorType">Actor type (e.g. <c>WorkstationSession</c>, <c>AgentInstance</c>, <c>Human</c>).</param>
/// <param name="TargetScope">
/// Where to anchor the projection's target path. <c>repo</c> (default) means relative to the repo
/// root, so the projection ships with the workspace. <c>user</c> writes outside the repo (e.g.
/// <c>~/.claude/skills/</c>).
/// </param>
/// <param name="StripSupersetFields">
/// When <c>true</c> (default for every non-Claude projector), the projector removes vendor-superset
/// frontmatter fields that the target surface cannot use (Claude Code: <c>allowed-tools</c>,
/// <c>model</c>, <c>effort</c>, <c>disable-model-invocation</c>, etc.).
/// </param>
public sealed record ProjectionContext(
    string ActorId,
    string ActorType,
    ProjectionTargetScope TargetScope,
    bool StripSupersetFields);

/// <summary>Where the projector's target path is anchored.</summary>
public enum ProjectionTargetScope
{
    /// <summary>Repo-root-relative path (default for Cursor, Codex, repo-scoped Claude).</summary>
    Repo,

    /// <summary>Actor-home-relative path (e.g. <c>~/.claude/skills/&lt;name&gt;</c>, opt-in).</summary>
    User,
}
