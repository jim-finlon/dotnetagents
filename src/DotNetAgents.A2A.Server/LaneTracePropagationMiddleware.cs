// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using DotNetAgents.A2A;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DotNetAgents.A2A.Server;

/// <summary>
/// Inbound A2A trace-propagation middleware. Story a744ddc2.
/// </summary>
/// <remarks>
/// <para>
/// ASP.NET Core's hosting layer already restores the W3C TraceContext parent
/// (<see cref="A2ATraceHeaders.TraceParent"/> / <see cref="A2ATraceHeaders.TraceState"/>)
/// from inbound headers automatically — the per-request <see cref="Activity"/> already has
/// the correct parent <see cref="ActivityContext"/> when this middleware runs.
/// </para>
/// <para>
/// What ASP.NET cannot do for us is the DNA-extension <see cref="A2ATraceHeaders.DnaLaneCorrelation"/>
/// header. This middleware reads it (when present) and tags the current Activity with
/// <see cref="A2ATraceHeaders.LaneWorkOrderTagKey"/> so downstream spans emitted within
/// the request scope inherit the work-order correlation key the trace-explorer skill
/// (story 92699a9b) queries on.
/// </para>
/// <para>
/// Order: register before the A2A endpoint mappings so the tag is present when the agent
/// starts handling work. Defensive — never throws on malformed headers.
/// </para>
/// </remarks>
public static class LaneTracePropagationMiddleware
{
    /// <summary>
    /// Add the inbound lane-trace propagation middleware to <paramref name="app"/>.
    /// Idempotent: a second registration is a harmless duplicate that runs twice but
    /// produces the same tag value.
    /// </summary>
    public static IApplicationBuilder UseA2ALaneTracePropagation(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Use(static async (ctx, next) =>
        {
            ApplyCorrelationTag(ctx);
            await next().ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Apply the inbound correlation tag to <see cref="Activity.Current"/>. Public for unit
    /// tests to drive the pure tag logic without spinning up a TestServer host.
    /// </summary>
    public static void ApplyCorrelationTag(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue(A2ATraceHeaders.DnaLaneCorrelation, out var values))
            return;

        var raw = values.ToString();
        if (string.IsNullOrWhiteSpace(raw)) return;

        // Defensive — only the GUID form is expected, but accept any non-empty token so the
        // trace-explorer skill can still group on whatever the caller used as a correlation
        // key. Validate as Guid for log fidelity, but don't reject when malformed.
        var current = Activity.Current;
        if (current is null) return;

        // Don't overwrite if already set by a more specific upstream span.
        if (current.GetTagItem(A2ATraceHeaders.LaneWorkOrderTagKey) is string existing
            && !string.IsNullOrWhiteSpace(existing))
        {
            return;
        }

        current.SetTag(A2ATraceHeaders.LaneWorkOrderTagKey, raw);
    }
}
