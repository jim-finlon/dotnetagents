# DotNetAgents Analyzers

This package provides Roslyn analyzers and source generators for DotNetAgents to enhance developer experience with compile-time validation and IntelliSense improvements.

## Features

### Analyzers

#### WorkflowAnalyzer (DNA001)
Validates `StateGraph<T>` workflow definitions at compile time:

- **DNA00101**: Ensures workflow graphs have an entry point
- **DNA00102**: Ensures workflow graphs have at least one exit point
- **DNA00103**: Warns about unreachable nodes
- **DNA00104**: Warns about nodes without outgoing edges that aren't exit points

#### StateMachineAnalyzer (DNA002)
Validates `StateMachineBuilder<T>` state machine definitions at compile time:

- **DNA00201**: Ensures state machines have an initial state
- **DNA00202**: Validates that all transitions reference existing states
- **DNA00203**: Warns about unreachable states

### Source Generators

#### WorkflowSourceGenerator
Automatically generates visualization helpers for `StateGraph<T>` workflows, providing metadata for IDE visualization tools.

## Installation

Add the package reference to your project:

```xml
<ItemGroup>
  <PackageReference Include="DotNetAgents.Analyzers" Version="1.0.0" />
</ItemGroup>
```

## Usage

The analyzers run automatically during compilation. No additional configuration is required.

### Example: Workflow Validation

```csharp
var workflow = new StateGraph<MyState>()
    .AddNode("start", async (state, ct) => state)
    .AddNode("process", async (state, ct) => state)
    .AddEdge("start", "process")
    // Missing SetEntryPoint() - DNA00101 error
    // Missing AddExitPoint() - DNA00102 error
    .Build();
```

### Example: State Machine Validation

```csharp
var stateMachine = new StateMachineBuilder<MyState>()
    .AddState("Idle")
    .AddState("Working")
    .AddTransition("Idle", "Working")
    // Missing SetInitialState() - DNA00201 error
    .Build();
```

## IDE Integration

The analyzers provide:
- **Compile-time errors** for invalid configurations
- **Warnings** for potential issues
- **IntelliSense** improvements (when combined with IDE extensions)
- **Quick fixes** (future enhancement)

## Future Enhancements

- Chain composition analyzer
- Behavior tree analyzer
- Code fixes for common issues
- Enhanced IntelliSense with code completion
- Visual Studio extension integration
