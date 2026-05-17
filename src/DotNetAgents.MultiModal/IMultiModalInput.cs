namespace DotNetAgents.MultiModal;

/// <summary>
/// Input that can contain mixed content (text, images, audio, video, documents).
/// </summary>
public interface IMultiModalInput
{
    /// <summary>Ordered list of content parts.</summary>
    IReadOnlyList<IContentPart> Parts { get; }
}
