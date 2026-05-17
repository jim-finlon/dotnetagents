# DotNetAgents.Tasks

Task management capabilities for DotNetAgents workflows and agents.

## Overview

The `DotNetAgents.Tasks` package provides task tracking and management capabilities that integrate seamlessly with DotNetAgents workflows. Tasks can be created, updated, and tracked throughout workflow execution, with support for dependencies, priorities, and status management.

## Features

- **Task CRUD Operations**: Create, read, update, and delete tasks
- **Dependency Tracking**: Track task dependencies (`DependsOn`, `BlockedBy`)
- **Status Management**: Track task status (Pending, InProgress, Completed, etc.)
- **Priority Levels**: Assign priorities (Low, Medium, High, Critical)
- **Task Statistics**: Get statistics and completion percentages
- **Workflow Integration**: Seamlessly integrate with DotNetAgents workflows

## Quick Start

```csharp
using DotNetAgents.Tasks;
using DotNetAgents.Tasks.Models;

// Register services
services.AddDotNetAgentsTasks();

// Use task manager
var taskManager = serviceProvider.GetRequiredService<ITaskManager>();

// Create a task
var task = await taskManager.CreateTaskAsync(new WorkTask
{
    SessionId = "session-123",
    Content = "Process user input",
    Priority = TaskPriority.High
}, cancellationToken);

// Update task status
var updatedTask = await taskManager.UpdateTaskAsync(task with
{
    Status = TaskStatus.InProgress,
    StartedAt = DateTimeOffset.UtcNow
}, cancellationToken);

// Get task statistics
var stats = await taskManager.GetTaskStatisticsAsync("session-123", cancellationToken);
Console.WriteLine($"Completion: {stats.CompletionPercentage}%");
```

## Workflow Integration

Tasks can be created and updated directly from workflow nodes:

```csharp
var workflow = WorkflowBuilder<MyState>.Create()
    .AddNode("create_task", async (state, ct) =>
    {
        var taskManager = serviceProvider.GetRequiredService<ITaskManager>();
        var task = await taskManager.CreateTaskAsync(new WorkTask
        {
            SessionId = state.SessionId,
            WorkflowRunId = state.RunId,
            Content = "Process user input",
            Priority = TaskPriority.High
        }, ct);
        return state with { TaskId = task.Id };
    })
    .Build();
```

## Models

- **`WorkTask`**: Represents a work task with all properties
- **`TaskStatus`**: Enum for task status (Pending, InProgress, Completed, etc.)
- **`TaskPriority`**: Enum for task priority (Low, Medium, High, Critical)
- **`TaskStatistics`**: Statistics about tasks

## Storage

The package provides storage abstractions (`ITaskStore`) with implementations:
- `InMemoryTaskStore`: For testing and development
- `SqlServerTaskStore`: SQL Server database storage (via `DotNetAgents.Storage.SqlServer`)
- `PostgreSQLTaskStore`: PostgreSQL database storage (via `DotNetAgents.Storage.PostgreSQL`)

### Using Database Storage

```csharp
// SQL Server
services.AddSqlServerTaskStore(connectionString, tableName: "WorkTasks");

// PostgreSQL
services.AddPostgreSQLTaskStore(connectionString, tableName: "work_tasks");
```

## Documentation

For detailed API documentation, see the XML documentation comments in the code.
