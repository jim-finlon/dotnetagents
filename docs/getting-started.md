# Getting Started

DotNetAgents is a .NET 10 package train for building agent systems with normal
.NET application practices: dependency injection, hosting, configuration,
logging, tests, and package references.

## Prerequisites

- .NET 10 SDK
- A model provider or local model runtime when your agent calls an LLM
- A normal .NET editor and test runner

You can start without live provider credentials by using deterministic test
tools, in-memory services, or the public examples.

## Create A Small Host

```bash
dotnet new console -n MyAgentApp
cd MyAgentApp
dotnet add package DotNetAgents.Core --version 1.0.0-preview.1
dotnet add package DotNetAgents.Hosting --version 1.0.0-preview.1
dotnet add package DotNetAgents.Mcp.Server --version 1.0.0-preview.1
```

Use `DotNetAgents.Hosting` when your agent should run as an ASP.NET Core
service. Use `DotNetAgents.AgentFramework` when you want a simple application
shell. Add protocol packages only when another client or agent needs to call
your tools.

## Minimal Application Shape

Most DotNetAgents applications end up with the same basic shape:

```text
MyAgentApp/
  Program.cs
  appsettings.json
  Agents/
    SupportAgent.cs
  Tools/
    TicketTools.cs
  Workflows/
    TriageWorkflow.cs
  Tests/
    SupportAgentTests.cs
```

The names are not important. The separation is:

- `Agents` decide what work to attempt.
- `Tools` perform explicit operations.
- `Workflows` coordinate repeatable steps.
- `Tests` prove the loop works without a production dependency.

## Example: A Read-Only Tool

Start with read-only tools. They are easier to test and safer to expose.

```csharp
public sealed record LookupTicketRequest(string TicketId);

public sealed record LookupTicketResult(
    string TicketId,
    string Status,
    string Summary);

public sealed class TicketTools
{
    public Task<LookupTicketResult> LookupTicketAsync(
        LookupTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TicketId))
        {
            throw new ArgumentException("TicketId is required.", nameof(request));
        }

        return Task.FromResult(new LookupTicketResult(
            request.TicketId,
            "Open",
            "Customer asked for an update."));
    }
}
```

Once this works, expose it through the runtime shape you need: in-process agent
code, MCP, A2A, or a normal HTTP endpoint.

## Example: Preview Before Mutation

When a tool changes external state, split preview from commit:

```csharp
public sealed record DraftReplyRequest(string TicketId, string ReplyText);

public sealed record DraftReplyPreview(
    string TicketId,
    string ReplyText,
    bool RequiresApproval,
    string[] Warnings);
```

The agent can produce a preview first. A human, policy engine, or workflow step
can approve it. Only then should a separate command send the reply.

This pattern is a large part of the "why" behind DotNetAgents: an agent should
be able to reason and propose, while the application keeps authority,
traceability, and policy.

## Pick The First Pattern

Choose one primary pattern before adding more packages:

| Need | Start With |
| --- | --- |
| Simple task agent | `DotNetAgents.AgentFramework` |
| Explicit states and transitions | `DotNetAgents.Agents.StateMachines` |
| Tactical decision logic | `DotNetAgents.Agents.BehaviorTrees` |
| Repeatable process | `DotNetAgents.Workflow` |
| Human/tool client access | `DotNetAgents.Mcp.Server` |
| Agent-to-agent calls | `DotNetAgents.A2A.Server` and `DotNetAgents.A2A.Client` |
| Runtime traces and replayable evidence | `DotNetAgents.Runtime` |

Avoid starting with every package. A good first agent has one runtime shape,
one or two tools, deterministic tests, and clear output.

## Add A Tool Surface

Most useful agents call tools. Keep tools explicit:

- name the tool for the action it performs
- validate arguments before work starts
- return structured results
- add preview/confirm behavior for high-impact actions
- never pass raw secrets through arguments or logs

If a human-operated client will call the tools, expose MCP. If another agent
will call the service, expose A2A. A service can support both.

## Validate The First Loop

Before calling the system useful, verify:

- the agent starts locally
- the simplest tool call succeeds
- invalid arguments fail with clear guidance
- logs and traces include correlation ids or run ids
- secrets are not printed
- behavior can be tested without a live production dependency

Move to the examples repository when you want runnable starter projects instead
of package-level guidance.

## Next Steps

- Add `DotNetAgents.Mcp.Server` when an IDE, CLI, or dashboard should call your
  tools.
- Add `DotNetAgents.A2A.Server` when another agent should call your service.
- Add plugins only when the agent needs external systems.
- Add observability before you debug complex behavior.
- Add governance before you allow writes, sends, deletes, purchases, deploys, or
  customer-visible actions.
