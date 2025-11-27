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
