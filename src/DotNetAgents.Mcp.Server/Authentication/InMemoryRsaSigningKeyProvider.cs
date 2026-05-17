using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// In-process RSA signing-key provider intended for tests, dev, and bootstrap scenarios.
/// Generates a 2048-bit RSA key on construction and exposes it as the current signing key.
/// </summary>
/// <remarks>
/// <strong>Production services MUST NOT register this provider.</strong> The umbrella AC
/// requires signing keys to come from CredentialsAgent. This implementation exists so that:
/// <list type="bullet">
/// <item>unit/integration tests can exercise issuer + validator code paths without a CredentialsAgent dependency,</item>
/// <item>CredentialsAgent itself (sub-slice 4) has a bootstrap path while the signing-key vault is being established.</item>
/// </list>
/// </remarks>
public sealed class InMemoryRsaSigningKeyProvider : ISigningKeyProvider, IDisposable
{
    private readonly RSA _rsa;
    private readonly RsaSecurityKey _key;
    private readonly SigningCredentials _credentials;

    public InMemoryRsaSigningKeyProvider(string? keyId = null)
    {
        _rsa = RSA.Create(2048);
        _key = new RsaSecurityKey(_rsa) { KeyId = keyId ?? Guid.NewGuid().ToString("N") };
        _credentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256);
    }

    public SigningCredentials GetCurrentSigningCredentials() => _credentials;

    public IReadOnlyList<SecurityKey> GetValidationKeys() => new SecurityKey[] { _key };

    /// <summary>Never raised by the in-memory provider — the key is generated once and never rotated.</summary>
    public event EventHandler? KeyRotated
    {
        add { }
        remove { }
    }

    public void Dispose() => _rsa.Dispose();
}
