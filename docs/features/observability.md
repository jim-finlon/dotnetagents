# Feature: Observability

Observability lets you understand what agents did and why.

## What To Observe

- agent run start and completion
- workflow step transitions
- model calls
- tool calls
- policy denials
- retries and timeouts
- output validation results
- human approvals

## Logs, Traces, Metrics

Use all three:

- logs for discrete events and failures
- traces for connected work across model and tool calls
- metrics for rates, latency, cost, and error counts

## Basic Shape

```csharp
using var activity = activitySource.StartActivity("agent.tool.call");
activity?.SetTag("agent.tool.name", toolName);
activity?.SetTag("agent.run.id", runId);
```

Keep payloads out of tags unless they are explicitly safe.

## Implementation Checklist

- generate a run id
- propagate correlation ids
- redact secrets before logs
- record validation failures
- track model/tool latency separately
- expose health endpoints for services
- make smoke tests assert key telemetry events when practical

## Related Packages

- `DotNetAgents.Observability`
- `DotNetAgents.Observability.AspNetCore`
- `DotNetAgents.Observability.GenAi`
- `DotNetAgents.Hosting`
- `DotNetAgents.Runtime`
