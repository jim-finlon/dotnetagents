// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace DotNetAgents.Mcp.Server;

internal static class McpStreamableHttpSessions
{
    private static readonly ConcurrentDictionary<string, byte> ActiveSessions = new(StringComparer.Ordinal);

    public static string ResolveSessionId(HttpContext http, bool createIfMissing)
    {
        if (http.Request.Headers.TryGetValue("MCP-Session-Id", out var sessionHeader) &&
            !string.IsNullOrWhiteSpace(sessionHeader))
        {
            var existing = sessionHeader.ToString().Trim();
            ActiveSessions.TryAdd(existing, 0);
            return existing;
        }

        if (http.Request.Query.TryGetValue("sessionId", out var sessionQuery) &&
            !string.IsNullOrWhiteSpace(sessionQuery))
        {
            var existing = sessionQuery.ToString().Trim();
            ActiveSessions.TryAdd(existing, 0);
            return existing;
        }

        if (!createIfMissing)
            return string.Empty;

        var created = $"dna-{Guid.NewGuid():N}";
        ActiveSessions.TryAdd(created, 0);
        return created;
    }

    public static void RemoveSession(string? sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
            ActiveSessions.TryRemove(sessionId, out _);
    }
}
