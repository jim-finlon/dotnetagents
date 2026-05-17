namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Composes the three projection-time security checks specified in story SUC-16:
/// <list type="bullet">
///   <item><see cref="SpdxHeaderValidator"/> — refuse to emit a skill body that lacks a
///     recognised SPDX license identifier (when SPDX is configured as required).</item>
///   <item><see cref="ChecksumVerifier"/> — when an expected
///     <c>sha256:&lt;hex&gt;</c> is supplied (from
///     <c>dna.skill.capability-pack.v1</c> contents[].checksum), refuse to emit if the
///     current manifest body has drifted.</item>
///   <item><see cref="CredentialPatternRedactor"/> — scrub credential-shaped substrings
///     (Bearer tokens, GitHub PATs, OpenAI <c>sk-…</c> keys, JWTs, generic
///     <c>X-Api-Key</c> values, GUID-shaped secrets near credential terms) and emit one
///     <see cref="SecurityScrubReceipt"/> entry per redacted span.</item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// SUC-16 deliverable. The pipeline returns a <see cref="SecurityCheckOutcome"/>: when the
/// outcome is <c>Approved</c>, callers project the returned (possibly-redacted) manifest
/// uniformly across all 11 projector types so security guarantees do not depend on which
/// projector ran. A <c>Refused</c> outcome carries one or more reasons (SPDX failure,
/// checksum drift) so the orchestrator can log + skip the skill, not crash.
/// </para>
/// <para>
/// The auth escape hatch documented in CLAUDE.md is preserved: redaction only scrubs
/// credential <em>values</em> (alphanumeric tokens matching credential patterns). Documented
/// procedure names, env-var keys, and CredentialsAgent tool names are not affected.
/// </para>
/// </remarks>
public sealed class SkillSecurityPipeline
{
    private readonly SkillSecurityPipelineOptions _options;
    private readonly SpdxHeaderValidator _spdx;
    private readonly ChecksumVerifier _checksum;
    private readonly CredentialPatternRedactor _redactor;

    public SkillSecurityPipeline(SkillSecurityPipelineOptions? options = null)
    {
        _options = options ?? new SkillSecurityPipelineOptions();
        _spdx = new SpdxHeaderValidator(_options.RequireSpdxHeader);
        _checksum = new ChecksumVerifier();
        _redactor = new CredentialPatternRedactor();
    }

    /// <summary>
    /// Run all three security checks against <paramref name="manifest"/>.
    /// </summary>
    /// <param name="manifest">Loaded canonical skill manifest.</param>
    /// <param name="expectedChecksum">
    /// Optional <c>sha256:&lt;hex&gt;</c> for the manifest body (from a capability pack).
    /// When omitted, checksum verification is skipped.
    /// </param>
    public SecurityCheckOutcome Check(SkillManifest manifest, string? expectedChecksum = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var refusals = new List<string>();

        if (!_spdx.Validate(manifest.Body))
        {
            refusals.Add("spdx_header_missing");
        }
        if (expectedChecksum is not null && !_checksum.Verify(manifest.Body, expectedChecksum))
        {
            refusals.Add("checksum_drift");
        }

        if (refusals.Count > 0)
        {
            return new SecurityCheckOutcome(
                Approved: false,
                Manifest: manifest,
                Receipts: Array.Empty<SecurityScrubReceipt>(),
                RefusalReasons: refusals);
        }

        // Redact credential values. The redactor returns the scrubbed body plus zero or more
        // receipts so callers (and KnowledgeMemory) can replay what was scrubbed.
        var (scrubbedBody, bodyReceipts) = _redactor.Scrub(manifest.Body, source: "skill-body");
        var (scrubbedFrontmatter, fmReceipts) = _redactor.Scrub(manifest.FrontmatterRaw, source: "skill-frontmatter");

        var receipts = new List<SecurityScrubReceipt>(bodyReceipts.Count + fmReceipts.Count);
        receipts.AddRange(bodyReceipts);
        receipts.AddRange(fmReceipts);

        var sanitized = ReferenceEquals(scrubbedBody, manifest.Body) && ReferenceEquals(scrubbedFrontmatter, manifest.FrontmatterRaw)
            ? manifest
            : manifest with { Body = scrubbedBody, FrontmatterRaw = scrubbedFrontmatter };

        return new SecurityCheckOutcome(
            Approved: true,
            Manifest: sanitized,
            Receipts: receipts,
            RefusalReasons: Array.Empty<string>());
    }
}

/// <summary>Tunable behaviour for <see cref="SkillSecurityPipeline"/>.</summary>
/// <param name="RequireSpdxHeader">
/// When <c>true</c> (the default since story 69f73478 flipped it), the SPDX header check is
/// enforced and a canonical SKILL.md body must include a <c>SPDX-License-Identifier:</c> line
/// (an HTML comment immediately after the YAML frontmatter is the conventional placement) or the
/// projection orchestrator refuses to emit that skill. Tests that exercise unrelated pipeline
/// behaviour against legacy bodies opt out by constructing
/// <c>SkillSecurityPipelineOptions(RequireSpdxHeader: false)</c> explicitly.
/// </param>
public sealed record SkillSecurityPipelineOptions(bool RequireSpdxHeader = true);

/// <summary>Outcome of one <see cref="SkillSecurityPipeline.Check"/> call.</summary>
/// <param name="Approved">Whether the projection orchestrator may emit this skill.</param>
/// <param name="Manifest">Manifest to feed downstream projectors (possibly redacted).</param>
/// <param name="Receipts">Per-redacted-span receipts so the scrub action is auditable.</param>
/// <param name="RefusalReasons">Short refusal codes when <see cref="Approved"/> is <c>false</c>.</param>
public sealed record SecurityCheckOutcome(
    bool Approved,
    SkillManifest Manifest,
    IReadOnlyList<SecurityScrubReceipt> Receipts,
    IReadOnlyList<string> RefusalReasons);

/// <summary>
/// One entry describing a single credential-pattern redaction. Receipts do NOT include the
/// original secret value — only the pattern that fired and where it matched. Callers can
/// route receipts to KnowledgeMemory for lesson capture (per SecurityNotes) without leaking secrets.
/// </summary>
/// <param name="PatternId">Short pattern id (e.g. <c>openai_sk</c>, <c>github_pat</c>, <c>bearer</c>).</param>
/// <param name="Source">Where the match was found (<c>skill-body</c> or <c>skill-frontmatter</c>).</param>
/// <param name="MatchedRange">Inclusive [start, end) byte offsets in the source string.</param>
/// <param name="RedactedPlaceholder">The placeholder substituted in place (e.g. <c>***REDACTED:openai_sk***</c>).</param>
public sealed record SecurityScrubReceipt(
    string PatternId,
    string Source,
    (int Start, int End) MatchedRange,
    string RedactedPlaceholder);
