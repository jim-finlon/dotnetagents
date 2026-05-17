using Microsoft.IdentityModel.Tokens;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Per-service signing-key abstraction used by <see cref="JwtMcpPkceTokenIssuerBase"/> and
/// <see cref="JwtMcpPkceBearerValidator"/>. Implementations fetch the key from CredentialsAgent
/// (production) or generate it in-memory (tests, dev, CredentialsAgent bootstrap).
/// </summary>
/// <remarks>
/// <para>
/// Hot-reload of signing keys is required by the umbrella story acceptance criteria: rotation
/// must not drop in-flight requests. Implementations satisfy this by: (1) returning the current
/// key from <see cref="GetCurrentSigningCredentials"/> on every issuance, and (2) including any
/// recently-rotated keys in <see cref="GetValidationKeys"/> so tokens minted under the previous
/// key continue to validate until they expire naturally.
/// </para>
/// <para>
/// The <see cref="KeyRotated"/> event lets <see cref="JwtMcpPkceBearerValidator"/> invalidate
/// any cached validation results when the key set changes.
/// </para>
/// </remarks>
public interface ISigningKeyProvider
{
    /// <summary>Returns the credentials used to sign new tokens. Called on every issuance — implementations may cache internally but MUST reflect rotation.</summary>
    SigningCredentials GetCurrentSigningCredentials();

    /// <summary>
    /// Returns every <see cref="SecurityKey"/> the validator should accept. Includes the current
    /// signing key plus any recently-rotated predecessors that may still appear on in-flight tokens.
    /// </summary>
    IReadOnlyList<SecurityKey> GetValidationKeys();

    /// <summary>Raised when <see cref="GetCurrentSigningCredentials"/> begins returning a different key.</summary>
    event EventHandler? KeyRotated;
}
