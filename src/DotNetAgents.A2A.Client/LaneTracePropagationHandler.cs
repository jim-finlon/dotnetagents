// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using DotNetAgents.A2A;

namespace DotNetAgents.A2A.Client;

/// <summary>
/// Outbound A2A propagation handler. Story a744ddc2.
/// Injects W3C Trace Context (<c>traceparent</c> / <c>tracestate</c>) plus the DNA-extension
/// <c>dna-lane-correlation</c> header from the current <see cref="Activity"/> onto every
/// outbound A2A request. No-op when no parent activity is current.
/// </summary>
/// <remarks>
/// Pairs with <c>LaneTracePropagationMiddleware</c> in DotNetAgents.A2A.Server, which
/// extracts the same headers and restores the parent context on the receiving side.
///
/// Ordering: register this handler before any auth handler in the typed-HttpClient pipeline
/// so the propagation headers are present on the wire even when an auth handler short-circuits
/// the request — the receiving server uses the headers solely for tracing, not for authn.
/// </remarks>
public sealed class LaneTracePropagationHandler : DelegatingHandler
{
    /// <summary>
    /// Optional override for the work-order id when no current Activity carries it as a tag.
    /// Useful for callers that have a known work-order context but have not yet started a
    /// lane.* span (e.g., long-running background processors).
    /// </summary>
    public Func<Guid?>? WorkOrderIdProvider { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        InjectHeaders(request);
        return base.SendAsync(request, cancellationToken);
    }

    private void InjectHeaders(HttpRequestMessage request)
    {
        var activity = Activity.Current;
        if (activity is not null)
        {
            // W3C TraceContext format is exactly the standard headers — no custom encoding.
            // System.Diagnostics serializes ActivityContext into the wire format.
            var ctx = activity.Context;
            if (ctx != default)
            {
                SetIfMissing(request, A2ATraceHeaders.TraceParent, FormatTraceParent(ctx));
                if (!string.IsNullOrEmpty(ctx.TraceState))
                    SetIfMissing(request, A2ATraceHeaders.TraceState, ctx.TraceState!);
            }

            // dna-lane-correlation: prefer the current Activity's lane.work_order_id tag.
            var tagWorkOrder = activity.GetTagItem(A2ATraceHeaders.LaneWorkOrderTagKey) as string;
            if (!string.IsNullOrWhiteSpace(tagWorkOrder))
            {
                SetIfMissing(request, A2ATraceHeaders.DnaLaneCorrelation, tagWorkOrder);
                return;
            }
        }

        // Fall back to an explicit provider if the caller registered one.
        var providerWorkOrder = WorkOrderIdProvider?.Invoke();
        if (providerWorkOrder is { } guid && guid != Guid.Empty)
        {
            SetIfMissing(request, A2ATraceHeaders.DnaLaneCorrelation, guid.ToString());
        }
    }

    private static void SetIfMissing(HttpRequestMessage request, string name, string value)
    {
        if (request.Headers.Contains(name)) return;
        request.Headers.TryAddWithoutValidation(name, value);
    }

    /// <summary>
    /// Formats <paramref name="ctx"/> per the W3C TraceContext spec:
    /// <c>{version}-{trace-id}-{parent-id}-{trace-flags}</c>.
    /// </summary>
    private static string FormatTraceParent(ActivityContext ctx)
    {
        var flags = ((byte)ctx.TraceFlags).ToString("x2");
        return $"00-{ctx.TraceId.ToHexString()}-{ctx.SpanId.ToHexString()}-{flags}";
    }
}
