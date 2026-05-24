// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using DotNetAgents.Abstractions.Chains;

namespace DotNetAgents.Core.Chains;

public static class ChainTracing
{
    public const string ActivitySourceName = "DotNetAgents.Core.Chains";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static Activity? StartActivity(
        string spanName,
        string chainKind,
        RunnableOptions? options,
        Action<Activity>? enrich = null)
    {
        var activity = ActivitySource.StartActivity(spanName, ActivityKind.Internal);
        if (activity is null)
            return null;

        activity.SetTag("chain.kind", chainKind);

        var trace = options?.TraceEnvelope;
        if (trace is not null)
        {
            activity.SetTag("chain.id", trace.ChainId);
            SetIfPresent(activity, "chain.run.id", trace.RunId);
            SetIfPresent(activity, "agent.id", trace.AgentId);
            SetIfPresent(activity, "chain.step.id", trace.StepId);
            SetIfPresent(activity, "llm.prompt.ref", trace.PromptRef);
            SetIfPresent(activity, "tool.name", trace.ToolName);
            SetIfPresent(activity, "correlation.id", trace.CorrelationId);
            SetIfPresent(activity, "story.id", trace.StoryId);
        }

        enrich?.Invoke(activity);
        return activity;
    }

    private static void SetIfPresent(Activity activity, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            activity.SetTag(key, value);
    }
}
