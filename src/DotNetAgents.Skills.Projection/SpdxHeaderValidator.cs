using System.Text.RegularExpressions;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Checks a skill body for a recognised SPDX license identifier line. Per SUC-16 SecurityNotes,
/// projection refuses to emit a body that fails this check when the pipeline is configured to
/// require it. The default Phase-1 posture is <c>required=false</c> because the canonical
/// dna-skills/ tree predates the SPDX requirement; a follow-up will backfill headers and flip
/// the default.
/// </summary>
internal sealed class SpdxHeaderValidator
{
    // Matches "SPDX-License-Identifier: Apache-2.0" or "<!-- SPDX-License-Identifier: MIT -->" etc.
    private static readonly Regex SpdxRe = new(
        @"SPDX-License-Identifier:\s*[A-Za-z0-9\.\-+]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly bool _required;

    public SpdxHeaderValidator(bool required)
    {
        _required = required;
    }

    /// <summary>
    /// Returns <c>true</c> when the body either contains an SPDX identifier or SPDX enforcement
    /// is disabled. Returns <c>false</c> only when enforcement is on AND the body is missing
    /// an identifier.
    /// </summary>
    public bool Validate(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (!_required) return true;
        return SpdxRe.IsMatch(body);
    }
}
