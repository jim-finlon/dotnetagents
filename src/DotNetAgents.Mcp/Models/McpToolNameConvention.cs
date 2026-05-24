// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace DotNetAgents.Mcp.Models;

/// <summary>
/// Framework-enforced naming convention for MCP tools. Strict MCP clients (Cursor, Claude
/// remote MCP) filter tools whose names contain characters outside <c>[A-Za-z0-9_]</c>;
/// enforcing the same convention at registration time catches drift at service startup
/// instead of silently rewriting names on the wire. Replaces the wire-time
/// <c>SanitizeToolNameForStrictMcpClients</c> rewrite that previously lived in
/// <c>McpStreamableHttpExtensions</c>.
/// </summary>
public static class McpToolNameConvention
{
    /// <summary>Maximum allowed length for an MCP tool name.</summary>
    public const int MaxLength = 128;

    private static readonly Regex ValidPattern = new(
        "^[A-Za-z0-9_]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="name"/> matches the strict MCP client naming rule:
    /// non-empty, 1..<see cref="MaxLength"/> characters, each character from <c>[A-Za-z0-9_]</c>.
    /// </summary>
    public static bool IsValid(string? name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > MaxLength)
        {
            return false;
        }

        return ValidPattern.IsMatch(name);
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> with a concrete remediation hint when
    /// <paramref name="name"/> violates the naming rule. Callers at tool-registration sites
    /// should invoke this so invalid names surface as service-startup failures, not as silent
    /// wire-time rewrites.
    /// </summary>
    /// <param name="name">Tool name to validate.</param>
    /// <param name="paramName">Optional parameter name for the thrown exception.</param>
    public static void Validate(string? name, string paramName = "name")
    {
        if (IsValid(name))
        {
            return;
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(
                "MCP tool name is required and must be non-empty.",
                paramName);
        }

        if (name.Length > MaxLength)
        {
            throw new ArgumentException(
                $"MCP tool name '{name}' exceeds the {MaxLength}-character maximum enforced for strict MCP clients.",
                paramName);
        }

        throw new ArgumentException(
            $"MCP tool name '{name}' contains characters outside [A-Za-z0-9_]. Strict MCP clients (Cursor, Claude remote MCP) reject such names. Rewrite at registration time (for example, replace '.' with '_' to turn 'business.create_lead' into 'business_create_lead').",
            paramName);
    }

    /// <summary>
    /// Legacy normalization helper retained for migration paths that still receive dotted
    /// names from older clients. Prefer registering valid names directly and calling
    /// <see cref="Validate"/>; only call this from incoming-request shims during a transition.
    /// </summary>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name ?? string.Empty;
        }

        return name.Replace('.', '_');
    }
}
