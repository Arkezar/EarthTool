# EarthTool Agent Guidelines

## Build & Test Commands
- Build solution: `dotnet build EarthTool.sln`
- Build single project: `dotnet build EarthTool.<Project>/EarthTool.<Project>.csproj`
- Run all tests: `dotnet test EarthTool.sln`
- Run single test project: `dotnet test EarthTool.<Project>.Tests/EarthTool.<Project>.Tests.csproj`
- Clean solution: `dotnet clean EarthTool.sln`

## Project Structure
- Multi-project .NET 8 C# solution with CLI, GUI, and library modules
- Test projects use xUnit, AutoFixture, FluentAssertions
- GUI uses Avalonia UI with ReactiveUI MVVM pattern
- Namespace format: `EarthTool.<Module>.<SubNamespace>`

## Code Style (from .editorconfig)
- **Indentation**: 2 spaces, no tabs
- **Line endings**: CRLF
- **Naming**: PascalCase for types/methods, _camelCase for private fields, interfaces start with "I"
- **Braces**: Always use, new line before open brace
- **Var**: Use for built-in types, explicit for others
- **Nullability**: Enabled, use null-conditional operators
- **Expression bodies**: Preferred for properties/accessors, not methods

## Language Requirements
- **IMPORTANT**: Use ONLY English in all code comments, commit messages, and documentation
- No Polish or other languages allowed in any code artifacts
- Code should be self-documenting with clear English naming

## Error Handling
- Use exceptions for error conditions
- Prefer explicit null checks over silent failures
- Use nullable reference types appropriately