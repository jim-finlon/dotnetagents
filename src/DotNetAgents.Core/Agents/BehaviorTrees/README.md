# Behavior Tree Integration Files

**Note**: These files have been moved to avoid circular dependencies.

The file `ToolSelectionBehaviorTree.cs` depends on `DotNetAgents.Agents.BehaviorTrees`, which creates a circular dependency since `DotNetAgents.Agents.BehaviorTrees` depends on `DotNetAgents.Core`.

**Resolution**: This integration file should be moved to:
- A separate integration project (e.g., `DotNetAgents.Core.Integrations`)
- Or directly into the `DotNetAgents.Agents.BehaviorTrees` project
- Or into application-specific projects that need both Core and Agents

For now, this file is commented out or moved to break the circular dependency.
