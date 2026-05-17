namespace DotNetAgents.Skills;

/// <summary>
/// Narrow transport contract used by <see cref="CredentialsAgentSigningKeyProvider"/> to delegate
/// signing + public-key resolution to CredentialsAgent without pulling the full Credentials.Client
/// surface into <c>DotNetAgents.Skills</c>. The default implementation
/// lives in <c>DotNetAgents.Credentials.Client</c> as <c>HttpRemoteAgentCardSigner</c> and posts to
/// <c>/mcp/tools/call</c>; tests inject a fake.
/// </summary>
public interface IRemoteAgentCardSigner
{
    /// <summary>
    /// Sign <paramref name="canonicalPayload"/> with the agent identity card named by
    /// <paramref name="keyRef"/>. The remote signer is expected to keep the private key inside
    /// CredentialsAgent custody and return only an algorithm tag + base64 signature.
    /// </summary>
    Task<RemoteSignature> SignAsync(string keyRef, ReadOnlyMemory<byte> canonicalPayload, CancellationToken ct);

    /// <summary>
    /// Resolve the agent identity card's public key + algorithm tag for verification. Returns null
    /// when the agent is unknown to CredentialsAgent or has been revoked.
    /// </summary>
    Task<RemotePublicKey?> ExportPublicKeyAsync(string keyRef, CancellationToken ct);
}

/// <summary>One signature returned by <see cref="IRemoteAgentCardSigner.SignAsync"/>.</summary>
/// <param name="Alg">Algorithm tag matching the schema enum (<c>ed25519</c> | <c>rsa-pss-sha256</c>).</param>
/// <param name="Signature">Base64-encoded signature bytes.</param>
public sealed record RemoteSignature(string Alg, string Signature);

/// <summary>Public key + algorithm tag exported from a CredentialsAgent identity card.</summary>
/// <param name="Alg">Algorithm tag matching the schema enum (<c>ed25519</c> | <c>rsa-pss-sha256</c>).</param>
/// <param name="PublicKeyBase64">SubjectPublicKeyInfo DER bytes, base64-encoded.</param>
public sealed record RemotePublicKey(string Alg, string PublicKeyBase64);
