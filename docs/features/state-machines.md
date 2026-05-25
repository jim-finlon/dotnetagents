# Feature: State Machines

Use state machines when an agent or workflow has explicit lifecycle states and
allowed transitions.

## Good Fits

- ticket triage: `New -> Classified -> Drafted -> Approved -> Sent`
- document processing: `Uploaded -> Parsed -> Reviewed -> Exported`
- deployment assistant: `Proposed -> Validated -> Approved -> Applied`
- onboarding: `Started -> WaitingForInput -> Completed`

## Why Use A State Machine

State machines make agent behavior inspectable. The system can answer:

- what state is this item in?
- what transitions are allowed?
- why was a transition refused?
- what evidence was recorded at transition time?

## Basic Shape

```csharp
public enum TicketState
{
    New,
    Classified,
    Drafted,
    Approved,
    Sent
}

public sealed record TicketContext(
    string TicketId,
    TicketState State,
    string? DraftReply);
```

Define transitions separately from model reasoning. The agent may recommend a
transition; the state machine decides whether it is valid.

## Runnable Example

The public examples repository includes a local support-ticket state machine:

```bash
dotnet run --project examples/control-loops -- state-machine
```

It uses `AgentStateMachine<T>` with guarded transitions and transition history.

## Implementation Checklist

- define states as a closed set
- define allowed transitions
- validate required data before transition
- record transition evidence
- test invalid transitions
- keep side effects outside the transition validator

## Related Packages

- `DotNetAgents.Agents.StateMachines`
- `DotNetAgents.Workflow`
- `DotNetAgents.Runtime`
- `DotNetAgents.PreviewConfirm`
