<!-- SPDX-License-Identifier: Apache-2.0 -->

# DotNetAgents.Voice

Voice command processing package for DotNetAgents, enabling natural language
voice command classification and parsing for assistant-style agent
applications.

## Features

- **Intent Classification** - LLM-based classification of voice commands into structured intents
- **Command Parsing** - Extract structured intents from natural language
- **Intent Taxonomy** - Predefined taxonomy of domains, actions, and parameters
- **Missing Parameter Detection** - Automatically identifies missing required parameters
- **Confidence Scoring** - Provides confidence scores for intent classification

## Installation

```xml
<PackageReference Include="DotNetAgents.Voice" Version="1.0.0" />
```

## Quick Start

### 1. Register Services

```csharp
using DotNetAgents.Voice;
using DotNetAgents.Providers.OpenAI;

var services = new ServiceCollection();

// Register LLM provider (required for intent classification)
services.AddOpenAI(options =>
{
    options.ApiKey = "your-api-key";
    options.ModelName = "gpt-4";
});

// Register voice command services
services.AddVoiceCommands();
```

### 2. Parse Voice Commands

```csharp
using DotNetAgents.Voice.Parsing;
using DotNetAgents.Voice.IntentClassification;

var parser = serviceProvider.GetRequiredService<ICommandParser>();

// Parse a voice command
var intent = await parser.ParseAsync("create a note about the meeting");

Console.WriteLine($"Domain: {intent.Domain}");      // "notes"
Console.WriteLine($"Action: {intent.Action}");      // "create"
Console.WriteLine($"Confidence: {intent.Confidence}"); // 0.95
Console.WriteLine($"Is Complete: {intent.IsComplete}"); // true/false
Console.WriteLine($"Missing Parameters: {string.Join(", ", intent.MissingRequired)}");
```

### 3. Use Intent Classification Directly

```csharp
using DotNetAgents.Voice.IntentClassification;

var classifier = serviceProvider.GetRequiredService<IIntentClassifier>();

var intent = await classifier.ClassifyAsync("schedule meeting with John tomorrow at 2pm");

// Intent properties:
// - Domain: "calendar"
// - Action: "create_event"
// - Parameters: { "attendee": "John", "date": "tomorrow", "time": "2pm" }
// - TargetService: "calendar_service"
```

## Intent Taxonomy

### Supported Domains

- **notes** - Note-taking operations
- **tasks** - Task management (personal, team)
- **calendar** - Calendar events and reminders
- **business** - Business operations (invoices, clients, projects)
- **media** - Content generation
- **research** - Research and information gathering

### Supported Actions

- `create` - Create a new item
- `list` - List items
- `update` - Update an existing item
- `delete` - Delete an item
- `query` - Query information
- `analyze` - Analyze data
- `generate` - Generate content
- `schedule` - Schedule an event
- `reminder` - Create a reminder

### Example Intents

```csharp
// Notes
"create a note about the meeting"
  → Domain: "notes", Action: "create", Parameters: { "content": "meeting" }

// Tasks
"create a personal task to review the report"
  → Domain: "tasks", Action: "create", SubType: "personal", Parameters: { "title": "review the report" }

// Calendar
"schedule meeting with John tomorrow at 2pm"
  → Domain: "calendar", Action: "create_event", Parameters: { "attendee": "John", "date": "tomorrow", "time": "2pm" }

// Business
"create invoice for Acme Corp for five thousand dollars"
  → Domain: "business", Action: "create_invoice", Parameters: { "client": "Acme Corp", "amount": 5000 }
```

## Intent Model

```csharp
public record Intent
{
    public string Domain { get; init; }           // e.g., "tasks", "calendar"
    public string Action { get; init; }          // e.g., "create", "list"
    public string? SubType { get; init; }         // e.g., "personal", "team"
    public Dictionary<string, object> Parameters { get; init; }
    public List<string> MissingRequired { get; init; }
    public double Confidence { get; init; }       // 0.0 to 1.0
    public string? TargetService { get; init; }   // MCP service name
    public string? Tool { get; init; }            // Tool name
    public string FullName { get; }               // "domain.action" or "domain.action.subtype"
    public bool IsComplete { get; }               // No missing required parameters
}
```

## Custom Intent Classifier

You can provide a custom intent classifier implementation:

```csharp
public class CustomIntentClassifier : IIntentClassifier
{
    public Task<Intent> ClassifyAsync(string commandText, CancellationToken cancellationToken = default)
    {
        // Your custom classification logic
    }
}

// Register it
services.AddIntentClassifier<CustomIntentClassifier>();
```

## Configuration

```csharp
services.AddVoiceCommands(options =>
{
    options.UseStructuredOutput = true;
    options.ConfidenceThreshold = 0.7;
});
```

## Integration with DotNetAgents

The voice command processing integrates seamlessly with other DotNetAgents packages:

- **DotNetAgents.Core** - Uses `ILLMModel` for classification
- **DotNetAgents.Workflow** - Intents can be used in workflow nodes
- **DotNetAgents.Mcp** - `TargetService` / tool routing maps to MCP-registered services via `IMcpAdapterRouter` (see `DotNetAgents.Mcp` README)

## Examples

See the [samples](../samples/) directory for complete examples.

## License

MIT License - see LICENSE file for details.
