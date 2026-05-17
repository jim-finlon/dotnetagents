namespace DotNetAgents.Mcp.Auth;

/// <summary>
/// Resolves a CIMD URL to a parsed + validated <see cref="ClientMetadataDocument"/>.
/// Implementations cache resolved documents per <see cref="McpAuthOptions.ClientMetadataCacheDuration"/>.
/// </summary>
public interface IClientMetadataDocumentResolver
{
    /// <summary>
    /// Fetch and validate the CIMD at <paramref name="clientIdUrl"/>. On success returns a
    /// <see cref="ClientMetadataResolution"/> with the parsed document; on failure returns
    /// validation errors and a <c>null</c> document.
    /// </summary>
    Task<ClientMetadataResolution> ResolveAsync(Uri clientIdUrl, CancellationToken cancellationToken = default);
}

/// <param name="Document">The parsed metadata document, or <c>null</c> when validation failed.</param>
/// <param name="Errors">Validation errors. Empty when the document is acceptable.</param>
/// <param name="FromCache">True when the result came from the cache rather than a fresh fetch.</param>
public sealed record ClientMetadataResolution(
    ClientMetadataDocument? Document,
    IReadOnlyList<string> Errors,
    bool FromCache)
{
    public bool Succeeded => Document is not null && Errors.Count == 0;
}
