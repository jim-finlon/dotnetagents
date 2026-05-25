# Tutorial: First MCP Tool Server

This tutorial shows the smallest useful MCP service shape: one tool provider,
health endpoints, tool discovery, and a tool call.

## Why Build This

MCP is the right surface when humans, IDEs, CLIs, dashboards, or external tool
clients need to discover and call tools. The agent logic remains normal .NET
code; MCP is the edge contract.

## Create The Host

```bash
dotnet new web -n TicketTools
cd TicketTools
dotnet add package DotNetAgents.Mcp.Server --version 1.0.0-preview.1
```

## Model The Tool Result

```csharp
public sealed record TicketSummary(
    string TicketId,
    string Status,
    string Summary,
    string SuggestedNextAction);
```

## Implement A Tool Provider

The exact interface may evolve during preview, but the pattern should stay the
same: list tools, validate arguments, return structured results.

```csharp
public sealed class TicketToolProvider : IMcpToolProvider
{
    public Task<McpListToolsResponse> GetToolsAsync(
        McpListToolsRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new McpListToolsResponse
        {
            Tools =
            [
                new McpToolDefinition
                {
                    Name = "tickets.lookup",
                    Description = "Look up a ticket summary by id.",
                    Category = "tickets"
                }
            ],
            TotalCount = 1
        });
    }

    public Task<McpToolCallResponse> CallToolAsync(
        McpToolCallRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Tool != "tickets.lookup")
        {
            return Task.FromResult(McpToolCallResponse.NotFound(request.Tool));
        }

        if (!request.Arguments.TryGetValue("ticketId", out var value) ||
            value is not string ticketId ||
            string.IsNullOrWhiteSpace(ticketId))
        {
            return Task.FromResult(McpToolCallResponse.InvalidArgument(
                request.Tool,
                "ticketId",
                "ticketId is required."));
        }

        var result = new TicketSummary(
            ticketId,
            "Open",
            "Customer asked for an update.",
            "Draft a reply, then request approval before sending.");

        return Task.FromResult(McpToolCallResponse.Success(result));
    }
}
```

If your preview package uses slightly different helper names, keep the same
contract behavior: unknown tools fail as not found, invalid arguments fail with
guidance, and successful calls return structured data.

## Map Endpoints

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMcpToolProvider, TicketToolProvider>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapMcpEndpoints(
    serviceName: "ticket_tools",
    mapHealth: false,
    instructionsBootstrap: "Ticket tool server for public DotNetAgents demos.");
app.MapMcpStreamableHttp("ticket_tools", "Ticket Tools", "1.0.0");

app.Run();
```

## Test The Shape

```bash
dotnet run
curl http://localhost:5000/health
curl http://localhost:5000/mcp/tools
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{"tool":"tickets.lookup","arguments":{"ticketId":"T-100"}}'
```

## Production Checklist

- Add auth before exposing non-local tools.
- Add preview/confirm for mutating tools.
- Keep secrets out of arguments and logs.
- Add tests for unknown tool and invalid argument behavior.
- Add OpenTelemetry spans around provider calls.
