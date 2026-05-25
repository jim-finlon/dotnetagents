# Core Concepts

DotNetAgents treats agents as software components. A model call can be part of
an agent, but the agent also needs identity, tools, state, policy, telemetry,
and tests.

## Agent

An agent is a component that receives a task or event, reasons over context,
selects tools or workflow steps, and returns structured output. In production,
the important question is not only what the model said. It is what the system
did, why it did it, and whether the result can be inspected.

## Tool

A tool is a callable operation with a name, schema, validation, and result
contract. Tools should be boring and explicit. The agent can decide when to use
them; the tool should still enforce its own safety rules.

## Workflow

A workflow is a repeatable sequence of steps, branches, approvals, retries, or
handoffs. Use workflows when the process matters as much as the agent's local
reasoning.

## Protocol Surface

DotNetAgents supports protocol surfaces so agents can be called from different
places:

- MCP for human-operated tools, developer environments, and external clients
- A2A for agent-to-agent communication
- HTTP or in-process APIs for normal application integration

Keep protocol DTOs neutral. Private product-specific operations should live
behind your own adapters.

## Memory And Context

Memory is not one thing. A production system usually separates:

- short-lived prompt context for one turn
- durable session state for resuming work
- knowledge or lessons reused across runs
- user profile or preference memory
- audit and trace evidence

DotNetAgents packages provide public contracts and local primitives. Your
application decides which durable stores are authoritative.

## Governance

Governance is how the system decides what it is allowed to do. Common controls
include:

- argument validation
- allowlists
- preview/confirm flows
- approval steps
- blast-radius classification
- policy predicates
- release gates

Add governance before connecting agents to expensive, destructive, private, or
customer-visible actions.

## Evidence

Self-improving systems need evidence. Capture enough information to answer:

- what task was attempted
- what inputs and tools were used
- what changed
- what tests or checks ran
- what failed
- what should change next

The public core gives you contracts and runtime shapes. Premium products may add
managed evidence, certification, and operations layers on top.

## Why This Matters

Agent systems fail differently than ordinary CRUD applications. A normal bug is
usually tied to a deterministic code path. An agent failure may involve a model
choice, a missing tool, weak retrieval, unclear policy, stale memory, bad
configuration, or a human approval gap.

DotNetAgents is organized around that reality:

- keep tools explicit so failures have boundaries
- keep workflows inspectable so long-running work can resume
- keep protocols separate so humans and agents call the right surface
- keep evidence structured so improvements can become reviewed changes
- keep governance close to the action so autonomy does not mean uncontrolled
  mutation

The "how" follows from the "why": build agents like application
infrastructure, not like scripts wrapped around a model call.
