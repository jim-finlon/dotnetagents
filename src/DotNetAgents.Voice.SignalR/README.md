<!-- SPDX-License-Identifier: Apache-2.0 -->

# DotNetAgents.Voice.SignalR

Real-time SignalR notifications for voice command processing in DotNetAgents, enabling JARVIS-like real-time status updates.

## Features

- **Real-time Status Updates** - Push status updates to clients as commands are processed
- **Clarification Requests** - Request missing parameters from users in real-time
- **Confirmation Requests** - Send read-back text for user confirmation
- **Completion Notifications** - Notify clients when commands complete
- **Error Notifications** - Push error messages to clients immediately
- **User & Command Groups** - Target notifications to specific users or commands

## Installation

```xml
<PackageReference Include="DotNetAgents.Voice.SignalR" Version="1.0.0" />
```

## Quick Start

### 1. Register SignalR Services

```csharp
using DotNetAgents.Voice.SignalR;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR
builder.Services.AddSignalR();

// Add command notifications
builder.Services.AddCommandNotifications();

var app = builder.Build();

// Map SignalR hub
app.MapHub<CommandStatusHub>("/hubs/command-status");

app.Run();
```

### 2. Use in Command Orchestrator

```csharp
using DotNetAgents.Voice.Orchestration;
using DotNetAgents.Voice.SignalR;

// Register orchestrator with notification service
services.AddScoped<ICommandWorkflowOrchestrator>(sp =>
{
    var parser = sp.GetRequiredService<ICommandParser>();
    var router = sp.GetRequiredService<IMcpAdapterRouter>();
    var logger = sp.GetRequiredService<ILogger<CommandWorkflowOrchestrator>>();
    var notificationService = sp.GetRequiredService<ICommandNotificationService>();

    return new CommandWorkflowOrchestrator(parser, router, logger, notificationService);
});
```

### 3. Client-Side JavaScript

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/command-status")
    .build();

// Subscribe to a command
await connection.invoke("SubscribeToCommand", commandId);

// Listen for status updates
connection.on("StatusUpdate", (update) => {
    console.log(`Command ${update.commandId}: ${update.status}`);
    console.log(`Message: ${update.message}`);
});

// Listen for clarification requests
connection.on("ClarificationRequest", (request) => {
    console.log(`Need ${request.missingParameter}: ${request.prompt}`);
    // Prompt user for input
});

// Listen for confirmation requests
connection.on("ConfirmationRequest", (request) => {
    console.log(`Confirm: ${request.readBackText}`);
    // Show confirmation dialog
});

// Listen for completion
connection.on("CommandCompleted", (update) => {
    console.log(`Command completed! Result:`, update.result);
});

// Listen for errors
connection.on("CommandError", (update) => {
    console.error(`Command failed: ${update.error}`);
});

await connection.start();
```

## API Reference

### ICommandNotificationService

```csharp
public interface ICommandNotificationService
{
    Task SendStatusUpdateAsync(
        Guid userId,
        Guid commandId,
        CommandStatus status,
        string? message = null,
        CancellationToken cancellationToken = default);

    Task SendClarificationRequestAsync(
        Guid userId,
        Guid commandId,
        string prompt,
        string missingParameter,
        int turn = 1,
        int maxTurns = 10,
        CancellationToken cancellationToken = default);

    Task SendConfirmationRequestAsync(
        Guid userId,
        Guid commandId,
        string readBackText,
        CancellationToken cancellationToken = default);

    Task SendCompletionAsync(
        Guid userId,
        Guid commandId,
        object? result,
        CancellationToken cancellationToken = default);

    Task SendErrorAsync(
        Guid userId,
        Guid commandId,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
```

### CommandStatusHub Methods

- `SubscribeToCommand(Guid commandId)` - Subscribe to updates for a specific command
- `UnsubscribeFromCommand(Guid commandId)` - Unsubscribe from command updates

### Client Events

- `StatusUpdate` - Status change notification
- `ClarificationRequest` - Request for missing parameter
- `ConfirmationRequest` - Request for user confirmation
- `CommandCompleted` - Command completed successfully
- `CommandError` - Command failed with error

## Integration with DotNetAgents

The SignalR notification service integrates seamlessly with:

- **DotNetAgents.Voice** - Command orchestration
- **DotNetAgents.Mcp** - MCP tool execution
- **DotNetAgents.Workflow** - Workflow state management

## Examples

See the [samples](../samples/) directory for complete examples.

## License

MIT License - see LICENSE file for details.
