# DotNetAgents REPL

Interactive Read-Eval-Print Loop (REPL) for testing and experimenting with DotNetAgents chains and workflows.

## Installation

```bash
dotnet tool install -g DotNetAgents.REPL
```

Or run directly:

```bash
cd src/DotNetAgents.REPL
dotnet run
```

## Usage

### Interactive Mode

Start an interactive session:

```bash
dotnet-agents-repl interactive
```

### Commands

#### Chain Testing

Test a chain with a prompt template:

```
dotnet-agents> chain "Hello {input}"|World
```

#### Workflow Execution

Execute a workflow:

```
dotnet-agents> workflow "test input"
```

#### Prompt Formatting

Format a prompt template:

```
dotnet-agents> prompt "Hello {name}, today is {day}"|name=Alice,day=Monday
```

#### Help

Show available commands:

```
dotnet-agents> help
```

#### Exit

Exit the REPL:

```
dotnet-agents> exit
```

## Examples

### Testing a Chain

```bash
dotnet-agents-repl chain "Translate to French: {input}" "Hello, world!"
```

### Interactive Session

```bash
$ dotnet-agents-repl interactive
DotNetAgents REPL
================
Type 'help' for commands, 'exit' to quit

dotnet-agents> chain "Hello {input}"|World
Prompt Template: Hello {input}
Input: World
Formatted Prompt: Hello World

dotnet-agents> workflow "process this"
Workflow execution:
Creating a simple workflow...
  [Node: start] Processing: process this
  [Node: process] Step 2
  [Node: end] Completed

Final State:
  input: process this
  step: 3
  message: Processing: process this
  processed: True
  completed: True

dotnet-agents> exit
Goodbye!
```

## Features

- **Chain Testing**: Test chains with different inputs
- **Workflow Execution**: Execute workflows interactively
- **Prompt Formatting**: Format prompt templates with variables
- **Interactive Mode**: Persistent session for experimentation
- **Error Handling**: Clear error messages

## Future Enhancements

- LLM integration for actual chain execution
- Workflow visualization
- History and command recall
- Variable persistence across commands
- Import/export workflows
