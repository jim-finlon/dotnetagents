using DotNetAgents.MultiModal.ContentParts;

namespace DotNetAgents.MultiModal;

/// <summary>
/// Default implementation of multi-modal input.
/// </summary>
public sealed class MultiModalInput : IMultiModalInput
{
    /// <inheritdoc />
    public IReadOnlyList<IContentPart> Parts { get; }

    /// <summary>Creates input from a list of parts.</summary>
    public MultiModalInput(IReadOnlyList<IContentPart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        Parts = parts;
    }

    /// <summary>Creates input from a single text part.</summary>
    public static IMultiModalInput FromText(string text)
    {
        return new MultiModalInput([new TextContentPart(text)]);
    }
}
