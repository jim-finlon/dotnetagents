# State Machine Integration Files

**Note**: These files have been moved to avoid circular dependencies.

The files in this directory (`AgentExecutionStateMachinePattern.cs`, `AgentExecutionStateMachineAdapter.cs`) depend on `DotNetAgents.Agents.StateMachines`, which creates a circular dependency since `DotNetAgents.Agents.BehaviorTrees` depends on `DotNetAgents.Core`.

**Resolution**: These integration files should be moved to:
- A separate integration project (e.g., `DotNetAgents.Core.Integrations`)
- Or directly into the `DotNetAgents.Agents.StateMachines` project
- Or into application-specific projects that need both Core and Agents

For now, these files are commented out or moved to break the circular dependency.
