// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Memory;

/// <summary>Stable public lesson identifier.</summary>
public readonly record struct LessonId
{
    public LessonId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Lesson id cannot be blank.", nameof(value));

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
