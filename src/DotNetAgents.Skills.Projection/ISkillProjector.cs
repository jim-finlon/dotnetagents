namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Projects a canonical DNA skill (sourced from <c>dna-skills/&lt;domain&gt;/&lt;name&gt;/SKILL.md</c>)
/// to a vendor-specific output path expected by a particular agent surface (Cursor, Claude
/// Code, Codex, Copilot, Goose, OpenHands, local-LLM tool-call, ...).
/// </summary>
/// <remarks>
/// Per the Skills Universal Catalog charter (epic 5baded15, docs/architecture/SKILLS-UNIVERSAL-CATALOG.md),
/// the canonical SKILL.md follows the Dec 2025 agentskills.io open standard. ~85 percent of
/// projections are file-routers (same content, different path); the lossy edges (Cursor rule,
/// Copilot path-scoped instructions, AGENTS.md graft, OpenAI tool JSON) have dedicated projector
/// implementations. This base contract covers both shapes via the
/// <see cref="SkillProjectionMode"/> on the returned <see cref="SkillProjection"/>.
/// </remarks>
public interface ISkillProjector
{
    /// <summary>
    /// Stable client identifier this projector targets (e.g. <c>cursor</c>, <c>claude-code</c>,
    /// <c>codex</c>, <c>copilot</c>, <c>goose</c>, <c>openhands</c>, <c>local-llm-tool</c>,
    /// <c>openai-tool</c>, <c>cursor-rule</c>, <c>copilot-instructions</c>, <c>agents-md</c>).
    /// </summary>
    string ClientKind { get; }

    /// <summary>
    /// Render the supplied canonical skill into the vendor-specific output.
    /// </summary>
    /// <param name="manifest">Canonical skill manifest sourced from <c>dna-skills/</c>.</param>
    /// <param name="context">Projection context (actor identity, repo vs user scope, etc.).</param>
    /// <returns>
    /// One <see cref="SkillProjection"/> representing the target path, file name, contents,
    /// mode (write or append-section), and any warnings raised during projection.
    /// </returns>
    SkillProjection Project(SkillManifest manifest, ProjectionContext context);
}
