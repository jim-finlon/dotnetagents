// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace DotNetAgents.Observability.GenAi;

/// <summary>
/// Emission sink for media-production ContextIntent v1 payloads (FR-SP705). Story edf42bd1
/// (P7.5 T4). The default sink attaches the serialized payload as an event + tag on
/// <see cref="Activity.Current"/> so OTel exporters can ship it without any extra plumbing;
/// tests can swap in a capturing sink via <see cref="Override"/>.
/// </summary>
public static class MediaProductionContextIntentSink
{
    /// <summary>Activity event name carrying the ContextIntent payload.</summary>
    public const string ActivityEventName = "dna.media.context_intent.v1";

    /// <summary>Activity / tag key carrying the JSON payload.</summary>
    public const string PayloadTag = "dna.context_intent.json";

    private static IMediaProductionContextIntentSink _sink = ActivitySink.Instance;

    /// <summary>The currently-installed sink (default: <see cref="ActivitySink"/>).</summary>
    public static IMediaProductionContextIntentSink Current => _sink;

    /// <summary>Replace the sink (e.g. with a capturing implementation in tests).</summary>
    public static IDisposable Override(IMediaProductionContextIntentSink replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        var previous = Interlocked.Exchange(ref _sink, replacement);
        return new SinkRestoreScope(previous);
    }

    /// <summary>Convenience pass-through.</summary>
    public static void Emit(ContextIntentV1 intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        _sink.Emit(intent);
    }

    private sealed class SinkRestoreScope : IDisposable
    {
        private readonly IMediaProductionContextIntentSink _previous;
        public SinkRestoreScope(IMediaProductionContextIntentSink previous) { _previous = previous; }
        public void Dispose() => Interlocked.Exchange(ref _sink, _previous);
    }

    /// <summary>Default <see cref="IMediaProductionContextIntentSink"/>: attach payload to Activity.Current.</summary>
    public sealed class ActivitySink : IMediaProductionContextIntentSink
    {
        public static readonly ActivitySink Instance = new();

        public void Emit(ContextIntentV1 intent)
        {
            var activity = Activity.Current;
            if (activity is null) return; // no listener — drop on the floor (operator may want a logger fallback later).

            var json = intent.ToJsonString();
            activity.SetTag(PayloadTag, json);
            activity.AddEvent(new ActivityEvent(
                name: ActivityEventName,
                timestamp: DateTimeOffset.UtcNow,
                tags: new ActivityTagsCollection
                {
                    [PayloadTag] = json,
                    ["task_id"] = intent.TaskId,
                    [MediaProductionAttributeNames.ActorId] = intent.Provenance.Actor.ActorId
                }));
        }
    }
}

/// <summary>Pluggable sink contract for media-production ContextIntent emission.</summary>
public interface IMediaProductionContextIntentSink
{
    /// <summary>Emit one payload at a tool/A2A task boundary.</summary>
    void Emit(ContextIntentV1 intent);
}
