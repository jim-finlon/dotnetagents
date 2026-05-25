# Feature: Workflows

Use workflows when agent behavior needs a repeatable process shape.

## Good Fits

- intake and triage
- document extraction
- publishing review
- CRM follow-up
- education feedback loops
- approval-oriented automation

## Workflow vs Agent

An agent chooses, reasons, or drafts. A workflow coordinates the process.

Good systems often use both:

1. workflow receives task
2. deterministic step validates inputs
3. agent drafts or classifies
4. workflow asks for approval
5. tool performs the action
6. workflow records evidence

## Basic Shape

```csharp
public interface IWorkflowStep<TState>
{
    Task<TState> RunAsync(TState state, CancellationToken cancellationToken);
}
```

Each step should have a small contract and clear failure behavior.

## Implementation Checklist

- model state explicitly
- keep steps small
- separate read-only and mutating steps
- record evidence after important steps
- make approval points visible
- test recovery from failed dependencies

## Related Packages

- `DotNetAgents.Workflow`
- `DotNetAgents.Tasks`
- `DotNetAgents.Agents.Tasks`
- `DotNetAgents.Runtime`
- `DotNetAgents.PreviewConfirm`
