# Tutorial: First Workflow Agent

This tutorial shows how to think about a workflow agent before adding advanced
runtime features.

## Why Build This

Agents are strongest when they make bounded decisions inside a process. A
workflow gives the process a durable shape: steps, checks, approvals, and
outputs.

## Scenario

Build a support triage workflow:

1. Receive a ticket.
2. Classify severity.
3. Look up account context.
4. Draft a response.
5. Require approval before sending.
6. Record the outcome.

## Define The State

```csharp
public sealed record TriageState(
    string TicketId,
    string CustomerMessage,
    string? Severity = null,
    string? AccountSummary = null,
    string? DraftReply = null,
    bool Approved = false);
```

## Define Workflow Steps

Keep each step small and testable:

```csharp
public interface ITriageStep
{
    Task<TriageState> RunAsync(TriageState state, CancellationToken ct);
}

public sealed class ClassifySeverityStep : ITriageStep
{
    public Task<TriageState> RunAsync(TriageState state, CancellationToken ct)
    {
        var severity = state.CustomerMessage.Contains("down", StringComparison.OrdinalIgnoreCase)
            ? "High"
            : "Normal";

        return Task.FromResult(state with { Severity = severity });
    }
}
```

## Add The Agent Decision Point

Use the agent where judgment is useful. Do not make the model own every step.

```csharp
public sealed class DraftReplyStep : ITriageStep
{
    public Task<TriageState> RunAsync(TriageState state, CancellationToken ct)
    {
        var draft = state.Severity == "High"
            ? "Thanks for the report. We are escalating this and will update you shortly."
            : "Thanks for reaching out. Here is the next step we recommend...";

        return Task.FromResult(state with { DraftReply = draft });
    }
}
```

Swap the deterministic draft logic for a model-backed implementation only after
the workflow contract is tested.

## Require Approval

```csharp
public sealed record ReplyPreview(
    string TicketId,
    string DraftReply,
    string Severity,
    bool RequiresApproval);
```

The workflow can stop at a preview and wait for approval. That is the practical
difference between a helpful agent and an uncontrolled automation.

## Validate

Tests should prove:

- severity classification has deterministic behavior
- account lookup failure returns a recoverable state
- draft creation does not send anything
- approval is required before external mutation
- outcome records contain non-secret evidence

## Where DotNetAgents Fits

Use:

- `DotNetAgents.Workflow` for repeatable process shape
- `DotNetAgents.Agents.StateMachines` when lifecycle states are explicit
- `DotNetAgents.Runtime` when you need trajectory and replay records
- `DotNetAgents.PreviewConfirm` for approval-oriented mutation paths
- `DotNetAgents.Observability.*` for traces and metrics
