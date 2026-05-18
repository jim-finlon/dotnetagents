namespace DotNetAgents.Abstractions.PublicSubstitutes.Tasks;

/// <summary>
/// Lightweight task/work-state surface for public examples.
/// Implementations must not expose private SDLC review, quality score, completion-policy, or lane metadata.
/// </summary>
public interface IPublicTaskStore
{
    ValueTask<PublicTaskHandle> StartAsync(
        PublicTaskRequest request,
        CancellationToken cancellationToken = default);

    ValueTask CompleteAsync(
        PublicTaskHandle handle,
        PublicTaskOutcome outcome,
        CancellationToken cancellationToken = default);

    ValueTask<PublicTaskRecord?> GetAsync(
        string id,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<PublicTaskRecord> ListAsync(
        PublicTaskQuery? query = null,
        CancellationToken cancellationToken = default);
}
