// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.Http;

namespace DotNetAgents.Mcp.Server;

internal static class McpStreamableHttpCorsPolicy
{
    public static bool ValidateOrigin(HttpContext http)
    {
        if (!http.Request.Headers.TryGetValue("Origin", out var origin) || string.IsNullOrEmpty(origin))
            return true;

        return IsAllowedOrigin(origin.ToString());
    }

    public static void ApplyCursorCorsHeaders(HttpContext http)
    {
        if (!http.Request.Headers.TryGetValue("Origin", out var origin) || string.IsNullOrEmpty(origin))
            return;

        var originValue = origin.ToString();
        if (!IsAllowedOrigin(originValue))
            return;

        if (HttpMethods.IsOptions(http.Request.Method))
        {
            var headers = http.Response.Headers;
            if (!headers.ContainsKey("Access-Control-Allow-Origin"))
                headers["Access-Control-Allow-Origin"] = originValue;
            if (!headers.ContainsKey("Vary"))
                headers["Vary"] = "Origin";
            var requestedMethod = http.Request.Headers["Access-Control-Request-Method"].ToString();
            var requestedHeaders = http.Request.Headers["Access-Control-Request-Headers"].ToString();
            headers["Access-Control-Allow-Methods"] = string.IsNullOrWhiteSpace(requestedMethod)
                ? "POST, GET, DELETE, OPTIONS"
                : requestedMethod;
            headers["Access-Control-Allow-Headers"] = string.IsNullOrWhiteSpace(requestedHeaders)
                ? "content-type"
                : requestedHeaders;
            headers["Access-Control-Max-Age"] = "600";
            if (string.Equals(http.Request.Headers["Access-Control-Request-Private-Network"], "true", StringComparison.OrdinalIgnoreCase))
                headers["Access-Control-Allow-Private-Network"] = "true";
            return;
        }

        http.Response.OnStarting(() =>
        {
            var headers = http.Response.Headers;
            if (!headers.ContainsKey("Access-Control-Allow-Origin"))
                headers["Access-Control-Allow-Origin"] = originValue;
            if (!headers.ContainsKey("Vary"))
                headers["Vary"] = "Origin";
            return Task.CompletedTask;
        });
    }

    private static bool IsAllowedOrigin(string o)
    {
        if (string.Equals(o, "null", StringComparison.OrdinalIgnoreCase) ||
            o.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
            o.StartsWith("vscode-webview://", StringComparison.OrdinalIgnoreCase))
            return true;

        if (Uri.TryCreate(o, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                host.Equals("cursor.com", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".cursor.com", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (o.StartsWith("cursor://", StringComparison.OrdinalIgnoreCase) ||
            o.StartsWith("vscode-file://", StringComparison.OrdinalIgnoreCase) ||
            o.StartsWith("vscode://", StringComparison.OrdinalIgnoreCase) ||
            o.StartsWith("https://cursor", StringComparison.OrdinalIgnoreCase) ||
            o.StartsWith("https://www.cursor", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
}
