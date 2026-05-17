namespace DotNetAgents.Abstractions.PublicSubstitutes.Memory;

/// <summary>
/// Stores public durable-memory lessons. Implementations may be in-memory,
/// local file-backed, premium, or private-factory backed.
/// </summary>
public interface IPublicLessonStore
{
    ValueTask<PublicLessonRecord> RecordAsync(
        PublicLessonRecord lesson,
        CancellationToken cancellationToken = default);

    ValueTask<PublicLessonRecord?> GetAsync(
        LessonId id,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<PublicLessonRecord> QueryAsync(
        PublicLessonQuery? query = null,
        CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(
        LessonId id,
        CancellationToken cancellationToken = default);
}
