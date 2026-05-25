# DotNetAgents.Hosting

Public-core ASP.NET Core host composition for DotNetAgents services.

## What this package owns

- Baseline `DotNetAgentsServiceHostOptions`-style configuration concepts:
  service name, display name, version, deployment ring, and health paths.
- `AddDnaServiceHost(...)` — binds options, registers ProblemDetails defaults, starts the
  startup-receipt hosted service.
- `MapDnaHealthEndpoints(...)` — maps `/health`, `/health/live`, `/health/ready` using the
  configured paths.
- Startup receipt contracts — redaction-safe startup receipts persisted in
  process memory for dashboards and audit packs.

## What this package does not own (yet)

- MCP and A2A route mapping (deferred to follow-up host-profile stories).
- external credential-store reference binding.
- OpenTelemetry resource defaults.
- Service-specific endpoint maps.

## Dependency policy

`DotNetAgents.Hosting` depends only on the ASP.NET Core framework reference. It must never
depend on `AgentProjects/*`, private factory packages, or service-specific infrastructure.

## Usage

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDnaServiceHost(builder.Configuration, builder.Environment, options =>
{
    options.ServiceName = "workflow-service";
    options.DisplayName = "WorkflowService";
    options.EnableProblemDetails = true;
    options.EnableStartupReceipt = true;
});

var app = builder.Build();
app.MapDnaHealthEndpoints();
app.Run();
```
