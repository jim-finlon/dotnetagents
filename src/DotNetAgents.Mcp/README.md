# DotNetAgents.Mcp

MCP (Model Context Protocol) client library for DotNetAgents, enabling integration with MCP-enabled services for JARVIS-like AI assistants.

## Features

- **MCP Client** - HTTP-based client for connecting to MCP services
- **Tool Discovery** - Discover and list tools from MCP services
- **Tool Execution** - Call tools on MCP services with proper error handling
- **Service Health** - Check health status of MCP services
- **Tool Registry** - Aggregate and cache tools from all registered services
- **Adapter Router** - Route intents to appropriate MCP services

## Installation

```xml
<PackageReference Include="DotNetAgents.Mcp" Version="1.0.0" />
```

## Quick Start

### 1. Register MCP Services

```csharp
using DotNetAgents.Mcp;
using DotNetAgents.Mcp.Configuration;

var services = new ServiceCollection();

// Register MCP clients
services.AddMcpClients(options =>
{
    // Add a service
    options.AddService("business_manager", "https://api.business.example.com", config =>
    {
        config.AuthType = "api_key";
        config.AuthToken = "your-api-key";
        config.TimeoutSeconds = 60;
    });

    // Add another service
    options.AddService("time_management", "https://api.time.example.com");
});
```

### 2. Use MCP Client Factory

```csharp
using DotNetAgents.Mcp.Abstractions;

var clientFactory = serviceProvider.GetRequiredService<IMcpClientFactory>();

// Get client for a service
var client = clientFactory.GetClient("business_manager");

// List available tools
var tools = await client.ListToolsAsync();
Console.WriteLine($"Found {tools.Tools.Count} tools");

// Call a tool
var response = await client.CallToolAsync(new McpToolCallRequest
{
    Tool = "create_invoice",
    Arguments = new Dictionary<string, object>
    {
        ["client"] = "Acme Corp",
        ["amount"] = 5000
    }
});

if (response.Success)
{
    Console.WriteLine($"Result: {response.Result}");
}
```

### 3. Use Tool Registry

```csharp
using DotNetAgents.Mcp.Abstractions;

var registry = serviceProvider.GetRequiredService<IMcpToolRegistry>();

// Get all tools from all services
var allTools = await registry.GetAllToolsAsync();

// Get tools for a specific service
var businessTools = await registry.GetToolsForServiceAsync("business_manager");

// Find a specific tool
var tool = await registry.FindToolAsync("create_invoice");
```

### 4. Use Adapter Router (with Voice Commands)

```csharp
using DotNetAgents.Mcp.Routing;
using DotNetAgents.Voice.IntentClassification;

var router = serviceProvider.GetRequiredService<IMcpAdapterRouter>();

// Parse a voice command
var parser = serviceProvider.GetRequiredService<ICommandParser>();
var intent = await parser.ParseAsync("create invoice for Acme Corp for $5000");

// Execute via adapter router
var result = await router.ExecuteIntentAsync(intent);
```

## MCP Service Configuration

```csharp
var config = new McpServiceConfig
{
    ServiceName = "my_service",
    BaseUrl = "https://api.example.com",
    AuthType = "jwt", // or "api_key", "none"
    AuthToken = "your-token",
    TimeoutSeconds = 30,
    RetryCount = 3,
    CircuitBreakerThreshold = 5,
    Headers = new Dictionary<string, string>
    {
        ["X-Custom-Header"] = "value"
    }
};
```

## Tool Definition

`McpToolDefinition` supports optional self-description fields for LLM consumers: `Examples`, `Category`, `RelatedTools`, and `UsageGuidance`. When MCP servers include these in tool listings, the registry returns them unchanged.

```csharp
var tool = new McpToolDefinition
{
    Name = "create_invoice",
    Description = "Creates a new invoice",
    ServiceName = "business_manager",
    InputSchema = new McpToolInputSchema
    {
        Type = "object",
        Properties = new Dictionary<string, McpProperty>
        {
            ["client"] = new McpProperty
            {
                Type = "string",
                Description = "Client name"
            },
            ["amount"] = new McpProperty
            {
                Type = "number",
                Description = "Invoice amount"
            }
        },
        Required = new List<string> { "client", "amount" }
    },
    // Optional: self-describing fields for tool discovery and guidance
    Category = "Billing",
    RelatedTools = new[] { "get_invoice", "list_invoices" },
    UsageGuidance = "Call after confirming client and amount; use get_invoice to retrieve the created invoice.",
    Examples = new[] { "create_invoice(client: \"Acme Corp\", amount: 5000)" }
};
```

## Mutating Tool Policy Wrapper

Process-critical MCP services can use `McpMutatingOperationPolicyWrapper` before running tools that create, update, delete, publish, deploy, claim, close, or otherwise mutate durable state. The wrapper evaluates non-secret workflow context such as actor id, actor type, story reference, correlation id, worktree path, branch, and approval receipts.

When required context is missing, the wrapper returns an `McpToolCallResponse` with `success = false`, `errorCode = MCP_MUTATING_OPERATION_PRECONDITIONS_MISSING`, `metadata.missingPreconditions`, and a machine-readable `McpRemediation`. This contract standardizes refusals without bypassing service-local authentication, authorization, or domain validation.

See `docs/sdlc-governance/MCP-MUTATING-OPERATION-POLICY-WRAPPER.md` for adoption guidance.

## Health Checking

```csharp
var client = clientFactory.GetClient("business_manager");
var health = await client.GetHealthAsync();

Console.WriteLine($"Status: {health.Status}"); // healthy, degraded, down
Console.WriteLine($"Latency: {health.LatencyMs}ms");
Console.WriteLine($"Available Tools: {health.AvailableTools}");
```

## Integration with DotNetAgents

The MCP client integrates seamlessly with other DotNetAgents packages:

- **DotNetAgents.Voice** - Routes intents to MCP services
- **DotNetAgents.Core** - Uses existing tool interfaces
- **DotNetAgents.Workflow** - Can be used in workflow nodes

## Examples

See the [samples](../samples/) directory for complete examples.

## License

MIT License - see LICENSE file for details.
