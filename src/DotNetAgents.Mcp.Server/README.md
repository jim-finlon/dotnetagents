# DotNetAgents.Mcp.Server

ASP.NET Core extensions for exposing MCP server endpoints so agents can implement the MCP HTTP contract (GET `/mcp/tools`, POST `/mcp/tools/call`) by implementing a single interface.

## Usage

1. **Reference the package** (or project reference to `DotNetAgents.Mcp.Server`).

2. **Implement `IMcpToolProvider`** — see `IMcpToolProvider.cs` (`GetToolsAsync`, `CallToolAsync`).

3. **Register and map endpoints**:

```csharp
builder.Services.AddSingleton<IMcpToolProvider, MyAgentMcpProvider>();

var app = builder.Build();
app.MapMcpEndpoints("my_service", mapHealth: true, instructionsBootstrap);
app.MapMcpStreamableHttp("my_service", "My Service", "1.0.0");
app.Run();
```

## Options

- `MapMcpEndpoints(mapHealth: true)` — when true (default), also maps GET `/health` to 200 OK.

## Contract (DNA / JARVIS HTTP)

- **GET /mcp/tools** — optional query: `category`, `limit`. Returns `McpListToolsResponse` (Tools, TotalCount).
- **POST /mcp/tools/call** — body: `McpToolCallRequest` (Tool, Arguments, CorrelationId?, TimeoutSeconds?). Returns `McpToolCallResponse`.

Failed `McpToolCallResponse` envelopes should include the first-class `remediation` object in addition to legacy `error`, `errorCode`, `guidance`, `suggestedNextSteps`, and `metadata` fields. `remediation` is the machine-readable recovery contract for agents and should set `remediationKind`, `serviceName`, `toolName`, `errorCode`, safe `guidance`, and `suggestedNextSteps`; validation failures should also set `invalidArgument` when known. Do not put raw secrets or credential values in this object.

## MCP Streamable HTTP (Cursor / Claude remote MCP)

For clients that speak **Model Context Protocol** over **Streamable HTTP** (JSON-RPC `initialize`, `tools/list`, `tools/call` on a **single path**), call **`MapMcpStreamableHttp`** after **`MapMcpEndpoints`**:

```csharp
app.MapMcpEndpoints("my_service", mapHealth: true, instructionsBootstrap);
app.MapMcpStreamableHttp("my_service", "My Service Display Name", "1.0.0"); // POST/GET/DELETE /mcp
```

- **POST /mcp** — JSON-RPC (same `IMcpToolProvider`). Legacy JARVIS continues to use `/mcp/tools` and `/mcp/tools/call`. Endpoints use **`DisableAntiforgery()`** so Blazor hosts with `UseAntiforgery()` still accept Cursor’s JSON `POST /mcp`.
- **GET /mcp** — returns **405** (no server SSE stream in this minimal implementation).

**Examples:** **OnlyOfficeBridge** (`5076`), **Infrastructure Control** (`5120`), **Security Scanning** (`5110`; requires `X-Api-Key` on `POST /mcp` when an API key is configured), plus core adapters in **knowledge-memory service**, **Credentials**, **TimeManagement**, **RepoIntelligence**, **Publishing**, **ProjectManagement**, **Education**, **MediaManagement**, **MediaProduction**, and **Sdlc**. Point Cursor `"url"` at `http://<host>:<port>/mcp` when deployed.

**Tests:** `tests/DotNetAgents.Mcp.Server.Tests` (transport regression against a stub `IMcpToolProvider`); agent APIs add integration tests in their own test projects.

See [docs/MCP-CONSUMER-PARITY-AND-ONBOARDING-PLAN.md](../../../docs/onboarding/mcp/MCP-CONSUMER-PARITY-AND-ONBOARDING-PLAN.md).

See [AgentProjects MCP-EXTENSION-PLAN](../../../AgentProjects/docs/MCP-EXTENSION-PLAN.md) and [MCP-CONTRACT-COMPATIBILITY](../../docs/guides/MCP-CONTRACT-COMPATIBILITY.md).
