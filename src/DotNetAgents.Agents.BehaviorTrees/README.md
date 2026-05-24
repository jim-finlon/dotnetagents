<!-- SPDX-License-Identifier: Apache-2.0 -->

# DotNetAgents.Agents.BehaviorTrees

Behavior tree implementation for hierarchical decision-making in autonomous agents.

## Overview

This library provides a comprehensive behavior tree system for managing complex agent decision-making. Behavior trees enable agents to make intelligent decisions through hierarchical node structures, supporting conditional logic, retries, timeouts, and integration with LLMs, workflows, and state machines.

## Features

- **Core Node Types**: Leaf nodes (Action, Condition), Composite nodes (Sequence, Selector, Parallel), Decorator nodes (Inverter, Repeater, UntilFail, Timeout, Cooldown, Retry, Conditional)
- **Integration Nodes**: LLM-based actions, Workflow execution, State machine integration
- **Execution Engine**: Observable execution with OpenTelemetry tracing and metrics
- **Builder Pattern**: Fluent API for constructing behavior trees
- **Observability**: Full OpenTelemetry integration

## Quick Start

### Basic Behavior Tree

```csharp
using DotNetAgents.Agents.BehaviorTrees;
using Microsoft.Extensions.Logging;

var logger = new LoggerFactory().CreateLogger<BehaviorTreeNode<MyContext>>();

// Create a simple sequence
var sequence = new SequenceNode<MyContext>("ProcessSequence", logger)
    .AddChild(new ActionNode<MyContext>("Action1", async (ctx, ct) => {
        // Do something
        return BehaviorTreeNodeStatus.Success;
    }, logger))
    .AddChild(new ActionNode<MyContext>("Action2", async (ctx, ct) => {
        // Do something else
        return BehaviorTreeNodeStatus.Success;
    }, logger));

// Create behavior tree
var tree = new BehaviorTree<MyContext>("MyTree", sequence);

// Execute
var context = new MyContext();
var status = await tree.ExecuteAsync(context);
```

### Using Builder

```csharp
var builder = new BehaviorTreeBuilder<MyContext>(logger);
var tree = builder
    .SetRoot(new SequenceNode<MyContext>("Root", logger)
        .AddChild(new ConditionNode<MyContext>("CheckCondition", ctx => ctx.IsValid, logger))
        .AddChild(new ActionNode<MyContext>("PerformAction", async (ctx, ct) => {
            // Action logic
            return BehaviorTreeNodeStatus.Success;
        }, logger)))
    .Build("MyTree");
```

### With Decorators

```csharp
var retryNode = new RetryNode<MyContext>("RetryAction", maxRetries: 3, TimeSpan.FromSeconds(1), logger)
    .SetChild(new ActionNode<MyContext>("Action", async (ctx, ct) => {
        // Action that might fail
        return BehaviorTreeNodeStatus.Success;
    }, logger));

var timeoutNode = new TimeoutNode<MyContext>("TimeoutAction", TimeSpan.FromSeconds(5), logger)
    .SetChild(retryNode);
```

### LLM Integration

```csharp
var llmNode = new LLMActionNode<MyContext>(
    "LLMAction",
    agentExecutor,
    contextToPrompt: ctx => $"Process: {ctx.Data}",
    resultToContext: (result, ctx) => { ctx.Result = result; return ctx; },
    logger);
```

### State Machine Integration

```csharp
// Check state machine state
var conditionNode = new StateMachineConditionNode<MyContext>(
    "CheckState",
    stateMachine,
    "Working",
    logger);

// Trigger state transition
var actionNode = new StateMachineActionNode<MyContext>(
    "TransitionState",
    stateMachine,
    "Idle",
    logger);
```

### Workflow Integration

```csharp
var workflowNode = new WorkflowActionNode<MyContext>(
    "ExecuteWorkflow",
    workflow,
    contextMapper: ctx => ctx,
    logger);
```

### Using Executor

```csharp
var executor = new BehaviorTreeExecutor<MyContext>(logger);
var result = await executor.ExecuteAsync(tree, context);

if (result.Status == BehaviorTreeNodeStatus.Success)
{
    Console.WriteLine($"Tree completed: {result.Message}");
}
```

## Architecture

### Node Types

**Leaf Nodes:**
- `ActionNode<TContext>`: Executes an action
- `ConditionNode<TContext>`: Evaluates a condition

**Composite Nodes:**
- `SequenceNode<TContext>`: Executes children sequentially until one fails
- `SelectorNode<TContext>`: Executes children until one succeeds
- `ParallelNode<TContext>`: Executes all children in parallel

**Decorator Nodes:**
- `InverterNode<TContext>`: Inverts Success/Failure
- `RepeaterNode<TContext>`: Repeats child N times
- `UntilFailNode<TContext>`: Repeats until child fails
- `TimeoutNode<TContext>`: Adds timeout to child
- `CooldownNode<TContext>`: Rate-limits child execution
- `RetryNode<TContext>`: Retries on failure with exponential backoff
- `ConditionalDecoratorNode<TContext>`: Only executes if condition is met

**Integration Nodes:**
- `LLMActionNode<TContext>`: Uses LLM for decision-making
- `WorkflowActionNode<TContext>`: Executes a workflow
- `StateMachineConditionNode<TContext>`: Checks state machine state
- `StateMachineActionNode<TContext>`: Triggers state machine transition

## Observability

Behavior trees integrate with OpenTelemetry:

- **Traces**: Node execution is traced with `BehaviorTreeActivitySource`
- **Metrics**:
  - `behavior_tree_executions_total`: Counter for tree executions
  - `behavior_tree_execution_duration_seconds`: Histogram for execution durations

## See Also

- [State Machines Documentation](../DotNetAgents.Agents.StateMachines/README.md)
- [Workflow Documentation](../DotNetAgents.Workflow/)
- [Agent Registry Documentation](../DotNetAgents.Agents.Registry/)
