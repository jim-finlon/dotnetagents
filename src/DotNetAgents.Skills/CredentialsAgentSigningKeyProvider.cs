// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using NSec.Cryptography;

namespace DotNetAgents.Skills;

/// <summary>
/// Production <see cref="ISigningKeyProvider"/> for capability-pack signing (story 589ed0b0). Sign()
/// delegates to CredentialsAgent's <c>sign_agent_card</c> MCP tool through the
/// <see cref="IRemoteAgentCardSigner"/> abstraction so the private key never leaves CredentialsAgent
/// custody. Verify() fetches the agent identity card's public key (via the same abstraction's
/// <see cref="IRemoteAgentCardSigner.ExportPublicKeyAsync"/>) and verifies locally.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ISigningKeyProvider.Sign"/> contract is synchronous (matches the in-memory test
/// provider). This implementation does a sync-over-async wrap on the remote call -- acceptable
/// for the per-signature network round-trip described in the story's RiskNotes, but callers that
/// sign many packs at once should batch through a dedicated async signer rather than relying on
/// this provider in tight loops.
/// </para>
/// <para>
/// Both algorithms permitted by the capability-pack schema (<c>rsa-pss-sha256</c> | <c>ed25519</c>)
/// are now verifiable locally. RSA-PSS-SHA256 uses the BCL via <see cref="RSA"/>; Ed25519 verify
/// (story 61eed818) uses <c>NSec.Cryptography</c> because the BCL does not ship an Ed25519
/// implementation on Linux as of net10.0. NSec bundles libsodium binaries cross-platform; malformed
/// keys or signatures return <c>false</c> rather than throwing.
/// </para>
/// </remarks>
public sealed class CredentialsAgentSigningKeyProvider : ISigningKeyProvider
{
    private readonly IRemoteAgentCardSigner _remote;

    /// <summary>Create a new provider bound to <paramref name="remote"/>.</summary>
    public CredentialsAgentSigningKeyProvider(IRemoteAgentCardSigner remote)
    {
        _remote = remote ?? throw new ArgumentNullException(nameof(remote));
    }

    /// <inheritdoc />
    /// <remarks>Synchronously awaits the remote <c>sign_agent_card</c> call. Never logs or
    /// otherwise echoes the signature value beyond returning it; never sees the private key.</remarks>
    public SignedPayload Sign(string keyRef, ReadOnlySpan<byte> canonicalPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyRef);
        // ReadOnlySpan<byte> cannot cross an async boundary; copy to a ReadOnlyMemory<byte> here.
        var payload = canonicalPayload.ToArray();
        var signature = _remote.SignAsync(keyRef, payload, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        if (signature is null)
            throw new InvalidOperationException(
                $"CredentialsAgent returned no signature for keyRef '{keyRef}'.");
        if (string.IsNullOrWhiteSpace(signature.Alg) || string.IsNullOrWhiteSpace(signature.Signature))
            throw new InvalidOperationException(
                $"CredentialsAgent signature for keyRef '{keyRef}' was missing alg or value.");
        return new SignedPayload(signature.Alg, signature.Signature);
    }

    /// <inheritdoc />
    public bool Verify(string keyRef, ReadOnlySpan<byte> canonicalPayload, ReadOnlySpan<byte> signature, string alg)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(alg);

        var payload = canonicalPayload.ToArray();
        var signatureBytes = signature.ToArray();
        var publicKey = _remote.ExportPublicKeyAsync(keyRef, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        if (publicKey is null) return false;
        if (!string.Equals(publicKey.Alg, alg, StringComparison.OrdinalIgnoreCase)) return false;

        switch (alg.ToLowerInvariant())
        {
            case "rsa-pss-sha256":
                return VerifyRsaPssSha256(publicKey.PublicKeyBase64, payload, signatureBytes);
            case "ed25519":
                return VerifyEd25519(publicKey.PublicKeyBase64, payload, signatureBytes);
            default:
                return false;
        }
    }

    private static bool VerifyRsaPssSha256(string publicKeyBase64, byte[] payload, byte[] signature)
    {
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(publicKeyBase64);
        }
        catch (FormatException)
        {
            return false;
        }
        using var rsa = RSA.Create();
        try
        {
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
        }
        catch (CryptographicException)
        {
            return false;
        }
        return rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    private static bool VerifyEd25519(string publicKeyBase64, byte[] payload, byte[] signature)
    {
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(publicKeyBase64);
        }
        catch (FormatException)
        {
            return false;
        }
        var algorithm = SignatureAlgorithm.Ed25519;
        NSec.Cryptography.PublicKey nsecPublicKey;
        try
        {
            nsecPublicKey = NSec.Cryptography.PublicKey.Import(algorithm, keyBytes, KeyBlobFormat.PkixPublicKey);
        }
        catch (FormatException)
        {
            return false;
        }
        return algorithm.Verify(nsecPublicKey, payload, signature);
    }
}
