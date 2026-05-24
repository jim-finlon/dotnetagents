// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.MultiModal.ContentParts;

/// <summary>Content part holding plain text.</summary>
public sealed class TextContentPart : IContentPart
{
    /// <inheritdoc />
    public ContentType Type => ContentType.Text;

    /// <inheritdoc />
    public object Content { get; }

    /// <summary>Creates a text part.</summary>
    public TextContentPart(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Content = text;
    }
}
