# Agent Guidelines for EarthTool

## Build/Test Commands
- **Build all**: `dotnet build EarthTool.sln --configuration Release`
- **Run all tests**: `dotnet test EarthTool.sln --configuration Release`
- **Run single test**: `dotnet test EarthTool.WD.Tests/EarthTool.WD.Tests.csproj --filter "FullyQualifiedName~WDExtractorTests"`
- **Format code**: `dotnet format EarthTool.sln --verify-no-changes`
- **CLI run**: `dotnet run --project EarthTool.CLI/EarthTool.CLI.csproj`
- **GUI run**: `dotnet run --project EarthTool.WD.GUI/EarthTool.WD.GUI.csproj`

## Code Style (from .editorconfig)
- **Indentation**: 2 spaces (not tabs)
- **Line endings**: LF only
- **Encoding**: UTF-8 with BOM
- **Private fields**: prefix with `_` (camelCase), e.g., `_logger`, `_archiveFactory`
- **Interfaces**: prefix with `I`, e.g., `IArchive`, `ICompressor`
- **Types/Methods**: PascalCase
- **Usings**: outside namespace, no grouping/sorting, use language keywords (`int` not `Int32`)
- **Braces**: always use braces for blocks, new line before open brace
- **Null handling**: use null-coalescing (`??`) and null-propagation (`?.`), enable nullable reference types
- **Patterns**: prefer pattern matching over `as`/`is` casting
- **Expression bodies**: prefer for accessors/properties/lambdas, avoid for methods/constructors

## Git Commits (Conventional Commits)
Follow format: `<type>(<scope>): <description>` where type = feat|fix|perf|refactor|docs|test|build|ci|chore, scope = cli|gui|wd|msh|dae|par|tex|common. Example: `feat(wd): add compression support`
- For commits that implement a tracked issue, include `Close #<issue-number>` in the commit body so merging the commit closes the issue automatically.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
