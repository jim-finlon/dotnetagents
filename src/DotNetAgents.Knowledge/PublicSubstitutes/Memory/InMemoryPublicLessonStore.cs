// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using DotNetAgents.Abstractions.PublicSubstitutes.Memory;

namespace DotNetAgents.Knowledge.PublicSubstitutes.Memory;

/// <summary>
/// Process-scoped public lesson store for examples, tests, and prototypes.
/// Lessons are lost when the process exits.
/// </summary>
public sealed class InMemoryPublicLessonStore : IPublicLessonStore
{
    private readonly ConcurrentDictionary<LessonId, PublicLessonRecord> _lessons = new();

    public ValueTask<PublicLessonRecord> RecordAsync(
        PublicLessonRecord lesson,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lesson);
        cancellationToken.ThrowIfCancellationRequested();

        var stored = Clone(lesson);
        _lessons[stored.Id] = stored;
        return ValueTask.FromResult(Clone(stored));
    }

    public ValueTask<PublicLessonRecord?> GetAsync(
        LessonId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_lessons.TryGetValue(id, out var lesson) ? Clone(lesson) : null);
    }

    public async IAsyncEnumerable<PublicLessonRecord> QueryAsync(
        PublicLessonQuery? query = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var matches = _lessons.Values
            .Where(lesson => Matches(lesson, query))
            .OrderByDescending(lesson => lesson.UpdatedAt)
            .ThenBy(lesson => lesson.Id.Value, StringComparer.Ordinal)
            .Take(query?.Limit is > 0 ? query.Limit.Value : int.MaxValue)
            .ToArray();

        foreach (var lesson in matches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return Clone(lesson);
            await Task.Yield();
        }
    }

    public ValueTask DeleteAsync(
        LessonId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _lessons.TryRemove(id, out _);
        return ValueTask.CompletedTask;
    }

    private static bool Matches(PublicLessonRecord lesson, PublicLessonQuery? query)
    {
        if (query is null)
            return true;

        if (!string.IsNullOrWhiteSpace(query.Namespace) &&
            !string.Equals(lesson.Namespace, query.Namespace.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (query.Tags is { Count: > 0 })
        {
            var lessonTags = lesson.Tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (query.Tags.Any(tag => string.IsNullOrWhiteSpace(tag) || !lessonTags.Contains(tag.Trim())))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            var text = query.Text.Trim();
            return lesson.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                   lesson.Body.Contains(text, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private static PublicLessonRecord Clone(PublicLessonRecord lesson) =>
        lesson with
        {
            Tags = lesson.Tags.ToArray(),
            Metadata = new Dictionary<string, string>(lesson.Metadata, StringComparer.OrdinalIgnoreCase),
        };
}
