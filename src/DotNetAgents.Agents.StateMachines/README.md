# DotNetAgents.Agents.StateMachines

State machine implementation for managing agent lifecycle and operational states in the DotNetAgents framework.

## Overview

This library provides a comprehensive state machine system for managing agent states, transitions, and lifecycle. It integrates seamlessly with the DotNetAgents framework, including workflows, agent registry, and observability.

## Features

- **Core State Machines**: Basic state machine with states, transitions, guards, and actions
- **Hierarchical State Machines**: Support for nested states (sub-states)
- **Parallel State Machines**: Support for orthogonal regions (parallel states)
- **Timed Transitions**: Automatic transitions based on timeouts or scheduled times
- **State Persistence**: Optional persistence for state machine state
- **Transition History**: Track state transition history
- **Observability**: Full OpenTelemetry integration with tracing and metrics
- **Workflow Integration**: State machine nodes for workflows
- **Multi-Agent Integration**: State-based worker pool selection and message bus integration

## Quick Start

### Basic State Machine

```csharp
using DotNetAgents.Agents.StateMachines;
using Microsoft.Extensions.Logging;

var logger = new LoggerFactory().CreateLogger<AgentStateMachine<MyState>>();
var builder = new StateMachineBuilder<MyState>(logger);

var stateMachine = builder
    .AddState("Idle")
    .AddState("Working")
    .AddTransition("Idle", "Working")
    .AddTransition("Working", "Idle")
    .SetInitialState("Idle")
    .Build();

var context = new MyState { Value = 1 };
await stateMachine.TransitionAsync("Working", context);
```

### State Machine with Guards

```csharp
var stateMachine = builder
    .AddState("Idle")
    .AddState("Working")
    .AddTransition("Idle", "Working", guard: s => s.Value > 0)
    .AddTransition("Working", "Idle")
    .SetInitialState("Idle")
    .Build();
```

### State Machine with Actions

```csharp
var stateMachine = builder
    .AddState("Idle", exitAction: s => Console.WriteLine("Leaving Idle"))
    .AddState("Working", entryAction: s => Console.WriteLine("Entering Working"))
    .AddTransition("Idle", "Working", onTransition: s => Console.WriteLine("Transitioning"))
    .SetInitialState("Idle")
    .Build();
```

### Using Common Patterns

```csharp
// Idle-Working pattern
var idleWorkingMachine = StateMachinePatterns.CreateIdleWorkingPattern<MyState>(logger).Build();

// Error-Recovery pattern
var errorRecoveryMachine = StateMachinePatterns.CreateErrorRecoveryPattern<MyState>(logger).Build();

// Worker Pool pattern with cooldown
var workerPoolMachine = StateMachinePatterns.CreateWorkerPoolPattern<MyState>(
    logger,
    cooldownDuration: TimeSpan.FromSeconds(5));
```

### Timeout Transitions

```csharp
var stateMachine = new AgentStateMachine<MyState>(logger);
stateMachine.AddState("Working");
stateMachine.AddState("Idle");
stateMachine.AddTimeoutTransition("Working", "Idle", TimeSpan.FromMinutes(5));
stateMachine.SetInitialState("Working");
```

### Scheduled Transitions

```csharp
var stateMachine = new AgentStateMachine<MyState>(logger);
stateMachine.AddState("Morning");
stateMachine.AddState("Afternoon");
var scheduledTime = DateTimeOffset.UtcNow.AddHours(12);
stateMachine.AddScheduledTransition("Morning", "Afternoon", scheduledTime);
stateMachine.SetInitialState("Morning");
```

### Integration with Agent Registry

```csharp
using DotNetAgents.Agents.Registry;

var agentRegistry = new InMemoryAgentRegistry();
var stateMachineRegistry = new AgentStateMachineRegistry<MyState>(agentRegistry, logger);

var stateMachine = StateMachinePatterns.CreateIdleWorkingPattern<MyState>(logger).Build();
await stateMachineRegistry.RegisterAsync("agent-1", stateMachine);

// Get agents in a specific state
var idleAgents = await stateMachineRegistry.GetAgentsByStateAsync("Idle");
```

### Integration with Workflows

```csharp
using DotNetAgents.Workflow.Graph;

var stateMachine = StateMachinePatterns.CreateIdleWorkingPattern<MyState>(logger).Build();

// Create workflow node that transitions state machine
var transitionNode = new StateMachineWorkflowNode<MyState>(
    "StartWork",
    stateMachine,
    "Working",
    logger);

// Create workflow node that checks state machine state
var conditionNode = new StateConditionWorkflowNode<MyState>(
    "CheckState",
    stateMachine,
    "Working",
    logger);
```

### Integration with Worker Pool

```csharp
using DotNetAgents.Agents.WorkerPool;

var baseWorkerPool = new WorkerPool(agentRegistry);
var stateBasedPool = new StateBasedWorkerPool(
    baseWorkerPool,
    stateMachineRegistry,
    logger);

// Get worker in specific state
var worker = await stateBasedPool.GetAvailableWorkerInStateAsync("Available");
```

### Message Bus Integration

```csharp
using DotNetAgents.Agents.Messaging;

var messageBus = new InMemoryAgentMessageBus();
var integration = new MessageBusStateMachineIntegration<MyState>(
    messageBus,
    stateMachineRegistry,
    logger);

// Subscribe to state transitions
var subscription = await integration.SubscribeToStateTransitionsAsync("agent-1");

// Send state transition request
await integration.SendStateTransitionRequestAsync("agent-1", "Working");
```

## Architecture

### Core Components

- **`IStateMachine<TState>`**: Core interface for state machines
- **`AgentStateMachine<TState>`**: Default implementation
- **`StateMachineBuilder<TState>`**: Fluent builder API
- **`HierarchicalStateMachine<TState>`**: Support for nested states
- **`ParallelStateMachine<TState>`**: Support for parallel regions

### Integration Components

- **`AgentStateMachineRegistry<TState>`**: Registry for managing multiple state machines
- **`StateMachineWorkflowNode<TState>`**: Workflow node for state transitions
- **`StateConditionWorkflowNode<TState>`**: Workflow node for state checks
- **`StateBasedWorkerPool`**: Worker pool with state-based selection
- **`MessageBusStateMachineIntegration<TState>`**: Message bus integration

### Patterns

- **`StateMachinePatterns`**: Common state machine patterns
  - `CreateIdleWorkingPattern`: Idle → Working → Idle
  - `CreateErrorRecoveryPattern`: Any → Error → Recovery → Idle
  - `CreateWorkflowStatePattern`: Uninitialized → Running → Completed/Failed
  - `CreateWorkerPoolPattern`: Available → Busy → CoolingDown → Available
  - `CreateSupervisorPattern`: Monitoring → Analyzing → Delegating → Waiting

## Observability

State machines integrate with OpenTelemetry for tracing and metrics:

- **Traces**: State transitions are traced with `StateMachineActivitySource`
- **Metrics**:
  - `state_transitions_total`: Counter for state transitions
  - `state_transition_duration_seconds`: Histogram for transition durations

## State Persistence

Implement `IStateMachinePersistence<TState>` to persist state machine state:

```csharp
public class MyStatePersistence : IStateMachinePersistence<MyState>
{
    public Task SaveStateAsync(string machineId, string state, MyState context, CancellationToken cancellationToken = default)
    {
        // Save state to database
    }

    public Task<SavedState<MyState>?> LoadStateAsync(string machineId, CancellationToken cancellationToken = default)
    {
        // Load state from database
    }

    public Task DeleteStateAsync(string machineId, CancellationToken cancellationToken = default)
    {
        // Delete state from database
    }
}
```

## Migration from AgentStatus Enum

State machines provide a more flexible alternative to the `AgentStatus` enum:

**Before:**
```csharp
agent.Status = AgentStatus.Busy;
```

**After:**
```csharp
await stateMachine.TransitionAsync("Busy", context);
```

State machines allow:
- Multiple states per agent
- Custom state names
- Guard conditions
- Entry/exit actions
- State history tracking

## See Also

- [Behavior Trees Documentation](../DotNetAgents.Agents.BehaviorTrees/README.md) (when implemented)
- [Workflow Documentation](../DotNetAgents.Workflow/)
- [Agent Registry Documentation](../DotNetAgents.Agents.Registry/)
