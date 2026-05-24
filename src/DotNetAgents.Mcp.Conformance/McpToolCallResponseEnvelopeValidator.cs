// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Conformance;

/// <summary>
/// Validates machine-readable <see cref="McpToolCallResponse"/> failure envelopes for MCP tool calls.
/// </summary>
public static class McpToolCallResponseEnvelopeValidator
{
    /// <summary>
    /// Returns validation violations when <paramref name="response"/> represents a failed tool call.
    /// Success responses return an empty list.
    /// </summary>
    public static IReadOnlyList<string> GetFailureViolations(McpToolCallResponse response)
    {
        if (response.Success)
            return Array.Empty<string>();

        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(response.Error))
            list.Add("Failed tool calls must set a non-empty Error message for operators and clients.");
        if (string.IsNullOrWhiteSpace(response.ErrorCode))
            list.Add("Failed tool calls must set a non-empty ErrorCode for programmatic handling.");
        return list;
    }

    /// <summary>
    /// Returns validation violations for the first-class machine-readable remediation payload.
    /// </summary>
    public static IReadOnlyList<string> GetFailureRemediationViolations(McpToolCallResponse response)
    {
        if (response.Success)
            return Array.Empty<string>();

        var list = new List<string>();
        if (response.Remediation is null)
        {
            list.Add("Failed tool calls should include Remediation for machine-readable recovery.");
            return list;
        }

        if (string.IsNullOrWhiteSpace(response.Remediation.RemediationKind))
            list.Add("Remediation must set RemediationKind.");
        if (string.IsNullOrWhiteSpace(response.Remediation.ErrorCode))
            list.Add("Remediation must mirror the response ErrorCode.");
        if (!string.Equals(response.ErrorCode, response.Remediation.ErrorCode, StringComparison.OrdinalIgnoreCase))
            list.Add("Remediation.ErrorCode must match the response ErrorCode.");
        if (string.IsNullOrWhiteSpace(response.Remediation.Guidance) && response.Remediation.SuggestedNextSteps.Count == 0)
            list.Add("Remediation must include Guidance or SuggestedNextSteps.");

        return list;
    }

    /// <summary>
    /// Returns true when the failure envelope is well-formed (both Error and ErrorCode populated).
    /// </summary>
    public static bool IsWellFormedFailure(McpToolCallResponse response, out IReadOnlyList<string> violations)
    {
        violations = GetFailureViolations(response);
        return violations.Count == 0;
    }

    /// <summary>
    /// Optional: successful calls should not carry stale error fields. Returns violations if Success is true but Error or ErrorCode are set.
    /// </summary>
    public static IReadOnlyList<string> GetSuccessHygieneViolations(McpToolCallResponse response)
    {
        if (!response.Success)
            return Array.Empty<string>();

        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(response.Error))
            list.Add("Successful tool calls should not set Error.");
        if (!string.IsNullOrWhiteSpace(response.ErrorCode))
            list.Add("Successful tool calls should not set ErrorCode.");
        if (response.Remediation is not null)
            list.Add("Successful tool calls should not set Remediation.");
        return list;
    }
}
