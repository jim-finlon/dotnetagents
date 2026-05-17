namespace DotNetAgents.A2A;

/// <summary>
/// Trace-context header names used by the DNA A2A propagation pair.
/// Story a744ddc2.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TraceParent"/> + <see cref="TraceState"/> are the W3C Trace Context standard
/// (https://www.w3.org/TR/trace-context/). They carry the OpenTelemetry parent
/// <see cref="System.Diagnostics.ActivityContext"/> across A2A boundaries so spans on the
/// caller and callee join into one trace.
/// </para>
/// <para>
/// <see cref="DnaLaneCorrelation"/> is a DNA-specific extension header carrying the
/// autonomous-lane work-order GUID. The server-side middleware copies its value into the
/// inbound <see cref="System.Diagnostics.Activity"/> as the <c>lane.work_order_id</c> tag
/// (mirroring <c>DotNetAgents.Observability.LaneOps.LaneTraceTags.WorkOrderId</c>) so
/// downstream spans stitch under the same correlation key the trace explorer queries on
/// (story 92699a9b).
/// </para>
/// </remarks>
public static class A2ATraceHeaders
{
    /// <summary>W3C Trace Context primary header.</summary>
    public const string TraceParent = "traceparent";

    /// <summary>W3C Trace Context vendor-state header.</summary>
    public const string TraceState = "tracestate";

    /// <summary>
    /// DNA extension header carrying the autonomous-lane work-order GUID. The server-side
    /// extractor tags incoming activities with <see cref="LaneWorkOrderTagKey"/> from this
    /// value when present.
    /// </summary>
    public const string DnaLaneCorrelation = "dna-lane-correlation";

    /// <summary>
    /// Activity-tag key set on the server-side incoming span when
    /// <see cref="DnaLaneCorrelation"/> is present. Matches
    /// <c>DotNetAgents.Observability.LaneOps.LaneTraceTags.WorkOrderId</c>.
    /// </summary>
    public const string LaneWorkOrderTagKey = "lane.work_order_id";
}
