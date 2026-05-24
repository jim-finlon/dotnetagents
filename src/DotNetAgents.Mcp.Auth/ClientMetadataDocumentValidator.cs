// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Mcp.Auth;

/// <summary>
/// Validates a <see cref="ClientMetadataDocument"/> against the MCP November 2025 minimum
/// shape and the operator-supplied <see cref="McpAuthOptions"/>.
/// </summary>
public static class ClientMetadataDocumentValidator
{
    /// <summary>
    /// Returns the list of validation errors. Empty result = document is acceptable.
    /// </summary>
    public static IReadOnlyList<string> Validate(
        ClientMetadataDocument? document,
        Uri fetchedFrom,
        McpAuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(fetchedFrom);
        ArgumentNullException.ThrowIfNull(options);

        if (document is null)
        {
            return new[] { "CIMD: document was null." };
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(document.ClientId))
        {
            errors.Add("CIMD: client_id is required.");
        }
        else if (!Uri.TryCreate(document.ClientId, UriKind.Absolute, out var clientIdUri))
        {
            errors.Add("CIMD: client_id must be an absolute URI.");
        }
        else if (!string.Equals(clientIdUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("CIMD: client_id URI must use https scheme.");
        }
        else if (!UrisAreEquivalent(clientIdUri, fetchedFrom))
        {
            errors.Add($"CIMD: client_id URI '{document.ClientId}' does not match the URL the document was fetched from '{fetchedFrom}'.");
        }

        if (document.RedirectUris.Count == 0)
        {
            errors.Add("CIMD: redirect_uris must contain at least one URI.");
        }
        else
        {
            foreach (var redirectUri in document.RedirectUris)
            {
                if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var ru))
                {
                    errors.Add($"CIMD: redirect_uri '{redirectUri}' is not a valid absolute URI.");
                    continue;
                }
                if (!string.Equals(ru.Scheme, "https", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(ru.Scheme, "http", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"CIMD: redirect_uri '{redirectUri}' must use http or https scheme.");
                }
            }
        }

        if (!document.GrantTypes.Contains("authorization_code", StringComparer.Ordinal))
        {
            errors.Add("CIMD: grant_types must include 'authorization_code' for MCP November 2025 PKCE flow.");
        }

        if (options.AllowedClientMetadataHosts.Count > 0)
        {
            var host = fetchedFrom.Host;
            if (!options.AllowedClientMetadataHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"CIMD: host '{host}' is not in the operator allow-list.");
            }
        }

        return errors;
    }

    private static bool UrisAreEquivalent(Uri a, Uri b)
    {
        return string.Equals(a.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase)
            && a.Port == b.Port
            && string.Equals(a.AbsolutePath.TrimEnd('/'), b.AbsolutePath.TrimEnd('/'), StringComparison.Ordinal);
    }
}
