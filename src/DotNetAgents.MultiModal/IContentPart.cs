// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.MultiModal;

/// <summary>
/// A single part of multi-modal content (text, image, audio, video, document, or file).
/// </summary>
public interface IContentPart
{
    /// <summary>Kind of content.</summary>
    ContentType Type { get; }

    /// <summary>Raw content: string for text, byte[] for binary, or Uri for references.</summary>
    object Content { get; }
}
