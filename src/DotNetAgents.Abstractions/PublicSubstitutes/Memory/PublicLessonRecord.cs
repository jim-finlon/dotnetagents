// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Memory;

/// <summary>
/// Public durable-memory lesson payload for open-core agents and examples.
/// Premium/private systems can adapt this contract to knowledge-memory service without
/// exposing private factory storage details.
/// </summary>
public sealed record PublicLessonRecord(
    LessonId Id,
    string Namespace,
    string Title,
    string Body,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
