namespace DotNetAgents.CLI.Scaffolding;

public sealed record ScaffoldResult(string RootPath, IReadOnlyList<string> CreatedFiles);
