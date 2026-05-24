// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.CLI.Scaffolding;

public sealed record ScaffoldResult(string RootPath, IReadOnlyList<string> CreatedFiles);
