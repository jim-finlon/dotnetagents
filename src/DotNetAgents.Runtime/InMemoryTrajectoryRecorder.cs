namespace DotNetAgents.Runtime;

public sealed class InMemoryTrajectoryRecorder : ITrajectoryRecorder
{
    private readonly object _gate = new();
    private readonly List<TrajectoryArtifact> _artifacts = [];

    public IReadOnlyList<TrajectoryArtifact> Artifacts
    {
        get
        {
            lock (_gate)
            {
                return [.. _artifacts];
            }
        }
    }

    public Task<TrajectoryArtifact> RecordAsync(
        AgentSessionActivity activity,
        ModelRouteMetadata modelRoute,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        cancellationToken.ThrowIfCancellationRequested();

        var artifactRefs = activity.Messages
            .Select(message => message.ContentRef)
            .Concat(activity.ToolInvocations.SelectMany(invocation => new[] { invocation.InputRef, invocation.OutputRef }))
            .Concat(activity.ProviderCalls.SelectMany(call => new[] { call.RequestRef, call.ResponseRef }))
            .Concat(activity.ContextSnapshots.Select(snapshot => snapshot.SnapshotRef))
            .Where(reference => reference is not null)
            .Cast<ArtifactReference>()
            .ToArray();

        var artifact = new TrajectoryArtifact
        {
            SessionId = activity.Session.Id,
            ActorId = activity.Session.ActorId,
            RunStatus = activity.Session.Status,
            ModelRoute = modelRoute,
            StartedAtUtc = activity.Session.CreatedAtUtc,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Messages = activity.Messages,
            ToolInvocations = activity.ToolInvocations,
            ProviderCalls = activity.ProviderCalls,
            ContextSnapshots = activity.ContextSnapshots,
            ArtifactRefs = artifactRefs
        };

        lock (_gate)
        {
            _artifacts.Add(artifact);
        }

        return Task.FromResult(artifact);
    }
}
