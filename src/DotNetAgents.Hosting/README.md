# DotNetAgents.Hosting

Public-core ASP.NET Core host composition for DNA services. Tranche 1 of the
service-host composition design (`docs/architecture/SERVICE-HOST-COMPOSITION-PACKAGE.md`).

## What this package owns

- Baseline `DnaServiceHostOptions` (service name, display name, version, deployment ring, health paths).
- `AddDnaServiceHost(...)` — binds options, registers ProblemDetails defaults, starts the
  startup-receipt hosted service.
- `MapDnaHealthEndpoints(...)` — maps `/health`, `/health/live`, `/health/ready` using the
  configured paths.
- `DnaStartupReceipt` + `IDnaStartupReceiptStore` — redaction-safe startup receipt persisted in
  process memory for dashboards and audit packs.

## What this package does not own (yet)

- MCP and A2A route mapping (deferred to follow-up host-profile stories).
- CredentialsAgent reference binding.
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
    options.ServiceName = "sdlc-agent";
    options.DisplayName = "WorkflowService";
    options.EnableProblemDetails = true;
    options.EnableStartupReceipt = true;
});

var app = builder.Build();
app.MapDnaHealthEndpoints();
app.Run();
```
