// SPDX-License-Identifier: Apache-2.0

using System.Net;
using System.Text;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Story 1095b26a. Pure HTML renderer for the PKCE consent page. No I/O,
/// deterministic — easy to unit test the markup contract. Output is a
/// minimal CSP-friendly HTML document with no inline scripts and no
/// external resources; the only interactive surface is the Allow / Deny
/// form POST. All caller-supplied values pass through
/// <see cref="WebUtility.HtmlEncode(string?)"/> so a hostile <c>client_id</c>
/// or <c>redirect_uri</c> can't inject markup.
/// </summary>
public static class PkceConsentPageRenderer
{
    /// <summary>
    /// Render the consent page HTML for a request. Caller embeds the
    /// returned string into the GET /.mcp/oauth/authorize response body.
    /// </summary>
    public static string Render(PkceConsentPageModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var sb = new StringBuilder(2048);
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta http-equiv=\"X-Content-Security-Policy\" content=\"default-src 'none'; style-src 'unsafe-inline'; form-action 'self'\">");
        sb.AppendLine("<title>Consent — DNA MCP</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:system-ui,-apple-system,sans-serif;background:#0f172a;color:#e2e8f0;margin:0;padding:2rem;}");
        sb.AppendLine(".card{max-width:640px;margin:2rem auto;background:#1e293b;border-radius:10px;padding:2rem;box-shadow:0 8px 24px rgba(0,0,0,0.3);}");
        sb.AppendLine("h1{font-size:1.4rem;margin:0 0 1rem;}");
        sb.AppendLine(".meta{font-family:ui-monospace,monospace;font-size:0.85rem;background:#0f172a;border-radius:6px;padding:0.75rem;margin-bottom:1rem;}");
        sb.AppendLine(".meta dt{color:#94a3b8;text-transform:uppercase;font-size:0.7rem;letter-spacing:0.06em;margin-top:0.5rem;}");
        sb.AppendLine(".meta dt:first-child{margin-top:0;}");
        sb.AppendLine(".meta dd{margin:0.1rem 0 0;}");
        sb.AppendLine(".scopes{list-style:none;padding:0;margin:0;}");
        sb.AppendLine(".scopes li{padding:0.3rem 0.5rem;background:#0f172a;border-radius:4px;margin:0.2rem 0;font-family:ui-monospace,monospace;font-size:0.85rem;}");
        sb.AppendLine(".actions{display:flex;gap:1rem;margin-top:1.5rem;}");
        sb.AppendLine("button{flex:1;padding:0.75rem 1rem;font-size:1rem;border:none;border-radius:6px;cursor:pointer;font-weight:600;}");
        sb.AppendLine(".allow{background:#22c55e;color:#0f172a;}");
        sb.AppendLine(".deny{background:#475569;color:#e2e8f0;}");
        sb.AppendLine(".note{font-size:0.78rem;color:#94a3b8;margin-top:1.5rem;}");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"card\">");
        sb.Append("<h1>Authorize ").Append(WebUtility.HtmlEncode(model.ClientDisplayName)).AppendLine("</h1>");
        sb.AppendLine("<dl class=\"meta\">");
        sb.AppendLine("<dt>Client</dt>");
        sb.Append("<dd>").Append(WebUtility.HtmlEncode(model.ClientId)).AppendLine("</dd>");
        sb.AppendLine("<dt>Service</dt>");
        sb.Append("<dd>").Append(WebUtility.HtmlEncode(model.ServiceName)).AppendLine("</dd>");
        sb.AppendLine("<dt>Operator</dt>");
        sb.Append("<dd>").Append(WebUtility.HtmlEncode(model.ActorId)).AppendLine("</dd>");
        sb.AppendLine("<dt>Redirect URI</dt>");
        sb.Append("<dd>").Append(WebUtility.HtmlEncode(model.RedirectUri)).AppendLine("</dd>");
        sb.AppendLine("</dl>");
        sb.AppendLine("<p>The application above is requesting access to:</p>");
        sb.AppendLine("<ul class=\"scopes\">");
        if (model.RequestedScopes.Count == 0)
        {
            sb.AppendLine("<li><em>(no scopes requested — access to default surface)</em></li>");
        }
        else
        {
            foreach (var scope in model.RequestedScopes)
            {
                sb.Append("<li>").Append(WebUtility.HtmlEncode(scope)).AppendLine("</li>");
            }
        }
        sb.AppendLine("</ul>");
        sb.Append("<form method=\"post\" action=\"").Append(WebUtility.HtmlEncode(model.DecisionPostPath)).AppendLine("\">");
        AppendHidden(sb, "client_id", model.ClientId);
        AppendHidden(sb, "redirect_uri", model.RedirectUri);
        AppendHidden(sb, "code_challenge", model.CodeChallenge);
        AppendHidden(sb, "code_challenge_method", model.CodeChallengeMethod);
        AppendHidden(sb, "scope", string.Join(' ', model.RequestedScopes));
        AppendHidden(sb, "state", model.State ?? "");
        AppendHidden(sb, "actor_id", model.ActorId);
        sb.AppendLine("<div class=\"actions\">");
        sb.AppendLine("<button type=\"submit\" name=\"decision\" value=\"allow\" class=\"allow\">Allow</button>");
        sb.AppendLine("<button type=\"submit\" name=\"decision\" value=\"deny\" class=\"deny\">Deny</button>");
        sb.AppendLine("</div>");
        sb.AppendLine("</form>");
        sb.Append("<p class=\"note\">Approving this request grants the client a single-use authorization code valid for ")
            .Append(model.AuthorizationCodeTtlSeconds).AppendLine(" seconds. The decision is recorded for this operator + client and persists until revoked from <code>/.mcp/admin/oauth/consents</code>.</p>");
        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static void AppendHidden(StringBuilder sb, string name, string value)
    {
        sb.Append("<input type=\"hidden\" name=\"")
            .Append(WebUtility.HtmlEncode(name))
            .Append("\" value=\"")
            .Append(WebUtility.HtmlEncode(value))
            .AppendLine("\">");
    }
}

/// <summary>Pure value bag for the consent-page renderer.</summary>
public sealed record PkceConsentPageModel(
    string ClientId,
    string ClientDisplayName,
    string ServiceName,
    string ActorId,
    string RedirectUri,
    string CodeChallenge,
    string CodeChallengeMethod,
    IReadOnlyList<string> RequestedScopes,
    string DecisionPostPath,
    string? State = null,
    int AuthorizationCodeTtlSeconds = 60);
