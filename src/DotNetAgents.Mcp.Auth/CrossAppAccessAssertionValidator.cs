namespace DotNetAgents.Mcp.Auth;

/// <summary>
/// Validates a parsed <see cref="CrossAppAccessAssertion"/> against the operator policy in
/// <see cref="McpAuthOptions"/>. Signature verification is the caller's job — DNA only owns
/// the policy + claim shape.
/// </summary>
public static class CrossAppAccessAssertionValidator
{
    public static IReadOnlyList<string> Validate(
        CrossAppAccessAssertion? assertion,
        McpAuthOptions options,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (assertion is null)
        {
            return new[] { "Cross App Access: assertion is null." };
        }

        if (!options.AllowCrossAppAccess)
        {
            return new[] { "Cross App Access: server policy disables cross-app access." };
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(assertion.Issuer))
        {
            errors.Add("Cross App Access: iss is required.");
        }

        if (string.IsNullOrWhiteSpace(assertion.Audience))
        {
            errors.Add("Cross App Access: aud is required.");
        }
        else if (options.CrossAppAccessAudiences.Count > 0
                 && !options.CrossAppAccessAudiences.Contains(assertion.Audience, StringComparer.Ordinal))
        {
            errors.Add($"Cross App Access: aud '{assertion.Audience}' not in operator allow-list.");
        }

        if (string.IsNullOrWhiteSpace(assertion.JwtId))
        {
            errors.Add("Cross App Access: jti is required for replay prevention.");
        }

        var nowUnix = nowUtc.ToUnixTimeSeconds();
        if (assertion.ExpirationUnix == 0 || assertion.ExpirationUnix < nowUnix)
        {
            errors.Add("Cross App Access: assertion is expired or has no exp claim.");
        }

        if (assertion.IssuedAtUnix > nowUnix + 60)
        {
            errors.Add("Cross App Access: iat is in the future (clock skew or replay).");
        }

        if (string.IsNullOrWhiteSpace(assertion.ClientMetadataUrl))
        {
            errors.Add("Cross App Access: mcp_client_metadata_url is required.");
        }
        else if (!Uri.TryCreate(assertion.ClientMetadataUrl, UriKind.Absolute, out var url)
                 || !string.Equals(url.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Cross App Access: mcp_client_metadata_url must be an absolute https URI.");
        }

        return errors;
    }
}
