// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.MultiModal.Models;

/// <summary>Output from a multi-modal model (e.g. chat completion with mixed content).</summary>
public sealed record MultiModalOutput
{
    /// <summary>Primary text content.</summary>
    public string? Text { get; init; }

    /// <summary>Additional content parts (e.g. audio bytes for TTS).</summary>
    public IReadOnlyList<IContentPart>? Parts { get; init; }
}
