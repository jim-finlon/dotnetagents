// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace DotNetAgents.Workflow.DecisionGraph;

/// <summary>
/// Pure edge-predicate evaluator for the v1 decision graph schema. Story 67a5c613.
/// Predicates are declarative — they look at run context state, the just-finished
/// node's result, and edge-specific config payload. No script execution. The
/// runtime calls <see cref="Evaluate"/> for each outgoing edge in order; the first
/// match wins.
/// </summary>
public static class EdgeEvaluator
{
    public static bool Evaluate(EdgeCondition condition, DecisionGraphRunContext context, DecisionGraphNode justFinished, NodeExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(context);

        return condition.Type switch
        {
            EdgeConditionType.Always => true,
            EdgeConditionType.StateExists => StateExists(condition, context),
            EdgeConditionType.StateEquals => StateEquals(condition, context),
            EdgeConditionType.ScoreAtLeast => ScoreAtLeast(condition, context),
            EdgeConditionType.IntentMatches => IntentMatches(condition, context),
            EdgeConditionType.ToolSucceeded => ToolOutcome(context, justFinished, expectSuccess: true),
            EdgeConditionType.ToolFailed => ToolOutcome(context, justFinished, expectSuccess: false),
            EdgeConditionType.PolicyAllowed => string.Equals(result.PolicyDecision, "allowed", StringComparison.Ordinal),
            EdgeConditionType.PolicyDenied => string.Equals(result.PolicyDecision, "denied", StringComparison.Ordinal),
            EdgeConditionType.NeedsConfirmation => string.Equals(result.PolicyDecision, "needsConfirmation", StringComparison.Ordinal),
            EdgeConditionType.LlmSchemaFieldEquals => LlmSchemaFieldEquals(condition, result),
            _ => false,
        };
    }

    private static bool StateExists(EdgeCondition cond, DecisionGraphRunContext ctx)
    {
        var path = TryGetExtraString(cond, "path");
        return path is not null && TryResolveStatePath(path, ctx) is not null;
    }

    private static bool StateEquals(EdgeCondition cond, DecisionGraphRunContext ctx)
    {
        var path = TryGetExtraString(cond, "path");
        if (path is null) return false;
        var actual = TryResolveStatePath(path, ctx);
        if (!cond.Extra!.TryGetValue("value", out var expected)) return false;
        return JsonValueEquals(actual, expected);
    }

    private static bool ScoreAtLeast(EdgeCondition cond, DecisionGraphRunContext ctx)
    {
        var path = TryGetExtraString(cond, "path") ?? "score";
        var threshold = cond.Extra!.TryGetValue("threshold", out var t) && t.TryGetDouble(out var d) ? d : 0.0;
        var actual = TryResolveStatePath(path, ctx);
        if (actual is double dd) return dd >= threshold;
        if (actual is int i) return i >= threshold;
        if (actual is JsonElement je && je.TryGetDouble(out var jd)) return jd >= threshold;
        return false;
    }

    private static bool IntentMatches(EdgeCondition cond, DecisionGraphRunContext ctx)
    {
        if (!cond.Extra!.TryGetValue("domains", out var domainsEl) || domainsEl.ValueKind != JsonValueKind.Array) return false;
        var allowed = domainsEl.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToHashSet(StringComparer.Ordinal);
        var intent = TryResolveStatePath("intent.domain", ctx) ?? TryResolveStatePath("intent", ctx);
        var actual = intent switch
        {
            string s => s,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
            _ => intent?.GetType().GetProperty("domain")?.GetValue(intent)?.ToString(),
        };
        return actual is not null && allowed.Contains(actual);
    }

    private static bool ToolOutcome(DecisionGraphRunContext ctx, DecisionGraphNode justFinished, bool expectSuccess)
    {
        // Look for a "toolResult.success" boolean either on the just-finished node's outputs
        // or in the run context state.
        var success = TryResolveStatePath("toolResult.success", ctx);
        if (success is bool b) return b == expectSuccess;
        if (success is JsonElement je && je.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return je.GetBoolean() == expectSuccess;
        return false;
    }

    private static bool LlmSchemaFieldEquals(EdgeCondition cond, NodeExecutionResult result)
    {
        var path = TryGetExtraString(cond, "path");
        if (path is null || result.Outputs is null) return false;
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        if (!result.Outputs.TryGetValue(parts[0], out var root) || root is null) return false;
        var current = root;
        for (var i = 1; i < parts.Length && current is not null; i++)
            current = ResolveProperty(current, parts[i]);
        if (!cond.Extra!.TryGetValue("value", out var expected)) return false;
        return JsonValueEquals(current, expected);
    }

    private static object? TryResolveStatePath(string path, DecisionGraphRunContext ctx)
    {
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;
        if (!ctx.State.TryGetValue(parts[0], out var current)) return null;
        for (var i = 1; i < parts.Length && current is not null; i++)
            current = ResolveProperty(current, parts[i]);
        return current;
    }

    private static object? ResolveProperty(object? container, string key)
    {
        if (container is null) return null;
        if (container is IDictionary<string, object?> dict) return dict.TryGetValue(key, out var v) ? v : null;
        if (container is JsonElement je && je.ValueKind == JsonValueKind.Object)
            return je.TryGetProperty(key, out var prop) ? (object)prop : null;
        var prop2 = container.GetType().GetProperty(key);
        return prop2?.GetValue(container);
    }

    private static string? TryGetExtraString(EdgeCondition cond, string key)
    {
        if (cond.Extra is null || !cond.Extra.TryGetValue(key, out var v)) return null;
        return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }

    private static bool JsonValueEquals(object? actual, JsonElement expected)
    {
        return expected.ValueKind switch
        {
            JsonValueKind.String => actual is string s && s == expected.GetString()
                                    || actual is JsonElement je && je.ValueKind == JsonValueKind.String && je.GetString() == expected.GetString(),
            JsonValueKind.True => actual is true || actual is JsonElement jt && jt.ValueKind == JsonValueKind.True,
            JsonValueKind.False => actual is false || actual is JsonElement jf && jf.ValueKind == JsonValueKind.False,
            JsonValueKind.Number => actual switch
            {
                int i when expected.TryGetInt32(out var ei) => i == ei,
                long l when expected.TryGetInt64(out var el) => l == el,
                double d when expected.TryGetDouble(out var ed) => Math.Abs(d - ed) < 1e-9,
                JsonElement je when je.TryGetDouble(out var jd) && expected.TryGetDouble(out var ed) => Math.Abs(jd - ed) < 1e-9,
                _ => false,
            },
            JsonValueKind.Null => actual is null,
            _ => false,
        };
    }
}
