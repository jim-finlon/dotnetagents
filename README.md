# DotNetAgents

DotNetAgents is a .NET framework for building agentic applications with
composable runtime primitives, protocol adapters, structured output, workflow,
tools, skills, memory, voice, observability, and agent pattern packages.

This repository contains the public core framework packages. Private factory
services, premium plugins, public plugin adapters, and application examples live
in separate repositories so the core framework stays focused and independently
buildable.

## Build

Restore and build the public solution:

```bash
dotnet restore DotNetAgents.Public.sln
dotnet build DotNetAgents.Public.sln --no-restore
```

## Package Set

The first public-core package set is listed in
[`PUBLIC-CORE-PACKAGES.txt`](PUBLIC-CORE-PACKAGES.txt). Source projects live
under [`src/`](src/).

## License

DotNetAgents public core is licensed under Apache-2.0. See
[`LICENSE`](LICENSE) and [`NOTICE`](NOTICE).
