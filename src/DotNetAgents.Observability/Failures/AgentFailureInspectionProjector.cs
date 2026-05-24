// SPDX-License-Identifier: Apache-2.0

using System.Text;

namespace DotNetAgents.Observability.Failures;

public static class AgentFailureInspectionProjector
{
    public static string RenderMarkdown(AgentFailureTelemetrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var builder = new StringBuilder();
        builder.AppendLine("# Agent Failure Telemetry");
        builder.AppendLine();
        builder.AppendLine($"Failures: `{snapshot.Failures.Count}`");
        builder.AppendLine($"Fallbacks: `{snapshot.Fallbacks.Count}`");
        builder.AppendLine($"Repeated patterns: `{snapshot.RepeatedPatterns.Count}`");
        builder.AppendLine();

        builder.AppendLine("## Repeated Patterns");
        builder.AppendLine();
        if (snapshot.RepeatedPatterns.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var pattern in snapshot.RepeatedPatterns)
                builder.AppendLine($"- `{pattern.Count}x` `{pattern.Kind}` `{pattern.HighestSeverity}` {pattern.Summary}");
        }
        builder.AppendLine();

        builder.AppendLine("## Recent Failures");
        builder.AppendLine();
        foreach (var failure in snapshot.Failures)
        {
            builder.AppendLine($"- `{failure.OccurredAtUtc:O}` `{failure.ActorId}` `{failure.Operation}` `{failure.Kind}` `{failure.Severity}` {failure.Summary}");
            if (!string.IsNullOrWhiteSpace(failure.Dependency))
                builder.AppendLine($"  Dependency: `{failure.Dependency}`");
            if (!string.IsNullOrWhiteSpace(failure.CorrelationId))
                builder.AppendLine($"  Correlation: `{failure.CorrelationId}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Fallbacks");
        builder.AppendLine();
        foreach (var fallback in snapshot.Fallbacks)
        {
            var status = fallback.Succeeded ? "succeeded" : "failed";
            builder.AppendLine($"- `{fallback.OccurredAtUtc:O}` `{fallback.Disposition}` {status}: {fallback.Action}");
        }

        return builder.ToString();
    }
}
