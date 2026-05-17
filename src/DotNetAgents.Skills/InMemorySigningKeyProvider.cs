using System.Security.Cryptography;

namespace DotNetAgents.Skills;

/// <summary>
/// In-memory RSA-PSS-SHA256 key provider used by the test suite and offline pack scoring. NOT for
/// production: real signing flows through CredentialsAgent so private keys never leave its
/// custody (see SUC-08 follow-up <c>77536c5f</c> SecurityNotes).
/// </summary>
/// <remarks>
/// <para>
/// The capability-pack schema permits two signature algorithms (<c>ed25519</c> | <c>rsa-pss-sha256</c>).
/// This provider ships RSA-PSS-SHA256 because .NET 10's BCL exposes it without a third-party
/// package dependency; an Ed25519 implementation can land alongside the CredentialsAgent bridge
/// (BouncyCastle / NSec) when that follow-up arrives.
/// </para>
/// </remarks>
public sealed class InMemorySigningKeyProvider : ISigningKeyProvider, IDisposable
{
    private readonly Dictionary<string, RSA> _keys = new();
    private bool _disposed;

    /// <summary>Generate (or rotate) an RSA-2048 keypair for <paramref name="keyRef"/>.</summary>
    /// <returns>Public key encoded as SPKI DER (base64-friendly via Convert.ToBase64String).</returns>
    public byte[] CreateOrRotate(string keyRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyRef);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_keys.TryGetValue(keyRef, out var existing))
        {
            existing.Dispose();
        }
        var rsa = RSA.Create(2048);
        _keys[keyRef] = rsa;
        return rsa.ExportSubjectPublicKeyInfo();
    }

    /// <summary>Return the SPKI-DER public key for <paramref name="keyRef"/>, or null if missing.</summary>
    public byte[]? GetPublicKey(string keyRef)
        => _keys.TryGetValue(keyRef, out var rsa) ? rsa.ExportSubjectPublicKeyInfo() : null;

    /// <inheritdoc />
    public SignedPayload Sign(string keyRef, ReadOnlySpan<byte> canonicalPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyRef);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_keys.TryGetValue(keyRef, out var rsa))
        {
            throw new InvalidOperationException($"No key registered for keyRef '{keyRef}'. Call CreateOrRotate first.");
        }
        var signature = rsa.SignData(
            canonicalPayload.ToArray(),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);
        return new SignedPayload(Alg: "rsa-pss-sha256", Signature: Convert.ToBase64String(signature));
    }

    /// <inheritdoc />
    public bool Verify(string keyRef, ReadOnlySpan<byte> canonicalPayload, ReadOnlySpan<byte> signature, string alg)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyRef);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!string.Equals(alg, "rsa-pss-sha256", StringComparison.Ordinal)) return false;
        if (!_keys.TryGetValue(keyRef, out var rsa)) return false;
        return rsa.VerifyData(
            canonicalPayload.ToArray(),
            signature.ToArray(),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var rsa in _keys.Values) rsa.Dispose();
        _keys.Clear();
        _disposed = true;
    }
}
