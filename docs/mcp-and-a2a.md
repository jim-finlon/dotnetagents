# MCP And A2A

DotNetAgents keeps human/tool surfaces and agent-to-agent surfaces separate.
That separation makes systems easier to secure and easier to reason about.

## MCP

Use MCP when the caller is a human-operated tool, developer environment,
dashboard, CLI, or external client that needs tool discovery and invocation.

Typical MCP service shape:

- `GET /mcp/instructions`
- `GET /mcp/tools`
- `POST /mcp/tools/call`
- optional Streamable HTTP MCP at `/mcp`
- `GET /health`

Use `DotNetAgents.Mcp.Server` to map the public endpoint shape and implement an
`IMcpToolProvider` for service-specific tools.

## A2A

Use A2A when the caller is another agent. A2A gives agents a stable way to
publish identity, capabilities, and callable operations without pretending that
every caller is a human tool client.

Typical A2A service shape:

- an agent card endpoint
- a typed task or message contract
- authentication suitable for your deployment
- traceable request/response behavior

## Choosing Between Them

| Caller | Prefer |
| --- | --- |
| IDE, CLI, dashboard, external tool client | MCP |
| Another agent or runtime service | A2A |
| Same process .NET component | in-process interface |
| Existing product API | HTTP API, optionally wrapped by MCP or A2A |

Many systems expose both MCP and A2A over the same underlying service logic.
Keep the service behavior in application code, then map separate protocol
adapters at the edge.

## Tool Safety Checklist

Before exposing a tool through either protocol:

- validate all arguments
- define a structured result
- make failures machine-readable
- redact secrets and sensitive values
- add preview/confirm for high-impact work
- include correlation ids or trace ids
- test unknown tool and invalid argument behavior

Protocol support makes agents reachable. It does not replace authorization,
policy, validation, or auditability.
