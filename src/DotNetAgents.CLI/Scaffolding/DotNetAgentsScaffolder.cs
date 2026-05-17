using System.Globalization;
using System.Text;

namespace DotNetAgents.CLI.Scaffolding;

public sealed class DotNetAgentsScaffolder
{
    private static readonly HashSet<string> ProjectTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        "agent",
        "chain",
        "workflow",
        "rag",
        "multi-agent",
        "mcp"
    };

    private readonly string _baseDirectory;

    public DotNetAgentsScaffolder(string? baseDirectory = null)
    {
        _baseDirectory = Path.GetFullPath(baseDirectory ?? Directory.GetCurrentDirectory());
    }

    public async Task<ScaffoldResult> CreateProjectAsync(string projectName, string template, CancellationToken cancellationToken = default)
    {
        var safeProjectName = ToIdentifier(projectName);
        var normalizedTemplate = NormalizeTemplate(template);
        var projectDirectory = Path.Combine(_baseDirectory, safeProjectName);
        Directory.CreateDirectory(projectDirectory);

        var files = new List<string>();
        await WriteFileAsync(files, projectDirectory, $"{safeProjectName}.csproj", ProjectFile(safeProjectName), cancellationToken).ConfigureAwait(false);
        await WriteFileAsync(files, projectDirectory, "Program.cs", ProgramFile(safeProjectName, normalizedTemplate), cancellationToken).ConfigureAwait(false);
        await WriteFileAsync(files, projectDirectory, "README.md", ReadmeFile(safeProjectName, normalizedTemplate), cancellationToken).ConfigureAwait(false);

        var component = normalizedTemplate switch
        {
            "chain" => ComponentTemplate("chain", "MainChain", safeProjectName),
            "workflow" => ComponentTemplate("workflow", "MainWorkflow", safeProjectName),
            "rag" => ComponentTemplate("tool", "RetrieveContextTool", safeProjectName),
            "multi-agent" => ComponentTemplate("agent", "CoordinatorAgent", safeProjectName),
            "mcp" => ComponentTemplate("tool", "McpHealthTool", safeProjectName),
            _ => ComponentTemplate("agent", "AssistantAgent", safeProjectName)
        };
        await WriteFileAsync(files, projectDirectory, component.RelativePath, component.Content, cancellationToken).ConfigureAwait(false);

        return new ScaffoldResult(projectDirectory, files);
    }

    public async Task<ScaffoldResult> AddComponentAsync(string component, string name, CancellationToken cancellationToken = default)
    {
        var normalizedComponent = component.Trim().ToLowerInvariant();
        if (normalizedComponent is not ("chain" or "workflow" or "agent" or "tool"))
        {
            throw new ArgumentException("Component must be one of: chain, workflow, agent, tool.", nameof(component));
        }

        var projectName = FindProjectName(_baseDirectory);
        var componentTemplate = ComponentTemplate(normalizedComponent, name, projectName);
        var files = new List<string>();
        await WriteFileAsync(files, _baseDirectory, componentTemplate.RelativePath, componentTemplate.Content, cancellationToken).ConfigureAwait(false);
        return new ScaffoldResult(_baseDirectory, files);
    }

    private static string NormalizeTemplate(string template)
    {
        var normalized = string.IsNullOrWhiteSpace(template) ? "agent" : template.Trim();
        if (!ProjectTemplates.Contains(normalized))
        {
            throw new ArgumentException(
                $"Template must be one of: {string.Join(", ", ProjectTemplates.Order(StringComparer.OrdinalIgnoreCase))}.",
                nameof(template));
        }

        return normalized.ToLowerInvariant();
    }

    private static async Task WriteFileAsync(
        ICollection<string> files,
        string root,
        string relativePath,
        string content,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (File.Exists(path))
        {
            throw new IOException($"Refusing to overwrite existing file: {path}");
        }

        await File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        files.Add(path);
    }

    private static string FindProjectName(string directory)
    {
        var project = Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        return project is null ? Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) : Path.GetFileNameWithoutExtension(project);
    }

    private static (string RelativePath, string Content) ComponentTemplate(string component, string name, string projectName)
    {
        var typeName = ToIdentifier(name);
        var ns = ToIdentifier(projectName);

        return component switch
        {
            "chain" => ($"Chains/{typeName}.cs", ChainTemplate(ns, typeName)),
            "workflow" => ($"Workflows/{typeName}.cs", WorkflowTemplate(ns, typeName)),
            "tool" => ($"Tools/{typeName}.cs", ToolTemplate(ns, typeName)),
            _ => ($"Agents/{typeName}.cs", AgentTemplate(ns, typeName))
        };
    }

    private static string ToIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(value));
        }

        var builder = new StringBuilder();
        var capitalizeNext = true;

        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                builder.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }

        var identifier = builder.Length == 0 ? "GeneratedAgent" : builder.ToString();
        if (char.IsDigit(identifier[0]))
        {
            identifier = $"Agent{identifier}";
        }

        return identifier;
    }

    private static string ProjectFile(string projectName) =>
        $$"""
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <RootNamespace>{{projectName}}</RootNamespace>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="DotNetAgents.Abstractions" Version="1.0.0" />
            <PackageReference Include="DotNetAgents.Core" Version="1.0.0" />
          </ItemGroup>

        </Project>
        """;

    private static string ProgramFile(string projectName, string template) =>
        $$"""
        Console.WriteLine("{{projectName}} is ready.");
        Console.WriteLine("Template: {{template}}");
        """;

    private static string ReadmeFile(string projectName, string template) =>
        $$"""
        # {{projectName}}

        Generated by `dotnet-agents new` using the `{{template}}` template.

        ## Build

        ```bash
        dotnet restore
        dotnet build
        ```

        Secrets should be referenced through CredentialsAgent categories or configuration keys. Do not paste raw credential values into generated code.
        """;

    private static string AgentTemplate(string ns, string typeName) =>
        $$"""
        using DotNetAgents.Abstractions.Agents;
        using DotNetAgents.Abstractions.Tools;

        namespace {{ns}}.Agents;

        public sealed class {{typeName}} : IAgent
        {
            public IReadOnlyList<ITool> AvailableTools { get; } = [];

            public Task<AgentStepResult> ExecuteStepAsync(string input, CancellationToken cancellationToken = default)
            {
                var output = string.IsNullOrWhiteSpace(input)
                    ? "Ready."
                    : $"Received: {input}";

                return Task.FromResult(new AgentStepResult
                {
                    Output = output,
                    ShouldContinue = false
                });
            }
        }
        """;

    private static string ChainTemplate(string ns, string typeName) =>
        $$"""
        using DotNetAgents.Core.Prompts;

        namespace {{ns}}.Chains;

        public sealed class {{typeName}}
        {
            private readonly PromptTemplate _prompt = new("Answer this request: {input}");

            public Task<string> FormatAsync(string input, CancellationToken cancellationToken = default) =>
                _prompt.FormatAsync(new Dictionary<string, object> { ["input"] = input }, cancellationToken);
        }
        """;

    private static string WorkflowTemplate(string ns, string typeName) =>
        $$"""
        namespace {{ns}}.Workflows;

        public sealed class {{typeName}}
        {
            public Task<IReadOnlyList<string>> RunAsync(string input, CancellationToken cancellationToken = default)
            {
                IReadOnlyList<string> steps =
                [
                    "receive",
                    string.IsNullOrWhiteSpace(input) ? "idle" : "process",
                    "respond"
                ];

                return Task.FromResult(steps);
            }
        }
        """;

    private static string ToolTemplate(string ns, string typeName) =>
        $$$"""
        using System.Text.Json;
        using DotNetAgents.Abstractions.Tools;

        namespace {{{ns}}}.Tools;

        public sealed class {{{typeName}}} : ITool
        {
            private const string ToolSchemaJson = "{\"type\":\"object\",\"properties\":{\"input\":{\"type\":\"string\"}" + "}}";
            private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(ToolSchemaJson);

            public string Name => "{{{typeName.ToLower(CultureInfo.InvariantCulture)}}}";

            public string Description => "Example tool scaffold.";

            public JsonElement InputSchema => Schema;

            public Task<ToolResult> ExecuteAsync(object input, CancellationToken cancellationToken = default) =>
                Task.FromResult(ToolResult.Success(new { message = "Tool executed.", input }));
        }
        """;
}
