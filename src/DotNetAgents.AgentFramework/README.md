# DotNetAgents.AgentFramework

Integration package for Microsoft Agent Framework compatibility.

## Overview

This package provides adapters and integration components to use DotNetAgents components (document loaders, tools, chains) with Microsoft Agent Framework agents and workflows.

## Status

**⚠️ Preview / Planning Phase**

Microsoft Agent Framework is currently in preview. This package will be developed as MAF stabilizes. The current implementation provides:

- Adapter interfaces and documentation
- Integration patterns and examples
- Migration guidance

## Planned Features

### Document Loader Tools
- Expose DotNetAgents document loaders (CSV, Excel, EPUB, PDF, Markdown) as MAF tools
- Enable agents to load and process documents seamlessly

### Chain Adapters
- Convert DotNetAgents chains to MAF-compatible workflows
- Bridge LangChain patterns to MAF patterns

### Tool Integration
- Expose DotNetAgents built-in tools (Calculator, DateTime, WebSearch, etc.) as MAF tools
- Enable tool discovery and registration

### Provider Integration
- Integrate DotNetAgents LLM providers with MAF agent execution
- Support for all 12+ providers through MAF

## Usage (Planned)

```csharp
// Register DotNetAgents components as MAF tools
services.AddDotNetAgents()
    .AddAgentFrameworkIntegration(options =>
    {
        // Expose document loaders as tools
        options.ExposeDocumentLoaders = true;

        // Expose built-in tools
        options.ExposeBuiltInTools = true;

        // Register LLM providers
        options.RegisterLLMProviders = true;
    });

// Use in MAF agent
var agent = new Agent("document-processor")
    .WithTool(new CsvDocumentLoaderTool())
    .WithTool(new PdfDocumentLoaderTool());
```

## Migration Path

1. **Phase 1**: Interface definitions and documentation (Current)
2. **Phase 2**: Basic tool adapters (When MAF API stabilizes)
3. **Phase 3**: Full integration with examples
4. **Phase 4**: Production-ready release

## Resources

- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/en-us/agent-framework/)
- [DotNetAgents Core Documentation](../DotNetAgents.Core/)
- [Integration Analysis](../../docs/architecture/microsoft-agent-framework-analysis.md)
