# Contributing to EarthTool

Thank you for your interest in contributing to EarthTool! This guide will help you get started.

## Table of Contents

- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
- [Coding Guidelines](#coding-guidelines)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing Requirements](#testing-requirements)
- [Documentation](#documentation)

## How Can I Contribute?

### Reporting Bugs

Before creating a bug report:

1. Check the [troubleshooting guide](docs/troubleshooting.md)
2. Search existing issues to avoid duplicates
3. Update to the latest version and verify the bug still exists

When creating a bug report, include:

- **Clear title**: Descriptive summary of the issue
- **Steps to reproduce**: Detailed steps to reproduce the behavior
- **Expected behavior**: What you expected to happen
- **Actual behavior**: What actually happened
- **Environment**: OS, .NET version, EarthTool version
- **Files**: Sample files that trigger the bug (if applicable)
- **Logs**: Relevant error messages or stack traces

**Template**:

```markdown
**Environment:**
- OS: Windows 11 / Ubuntu 22.04 / macOS 13
- .NET Version: 8.0.1
- EarthTool Version: 1.0.0

**Description:**
Clear description of the bug.

**Steps to Reproduce:**
1. Open application
2. Load archive `example.wd`
3. Click Extract All
4. Error occurs

**Expected Behavior:**
Files should be extracted without error.

**Actual Behavior:**
Application throws `OutOfMemoryException`.

**Additional Context:**
Archive size is 2GB.
```

### Suggesting Enhancements

Enhancement suggestions are welcome! Include:

- **Use case**: Why is this enhancement needed?
- **Proposed solution**: How should it work?
- **Alternatives**: Other approaches you considered
- **Examples**: Screenshots, mockups, or code examples

### Contributing Code

We welcome pull requests for:

- Bug fixes
- New features
- Performance improvements
- Documentation improvements
- Test coverage improvements

## Development Setup

### Prerequisites

- **.NET SDK 8.0** or later
- **Git**
- **IDE**: Visual Studio 2022, VS Code with C# extension, or JetBrains Rider

### Setup Steps

1. **Fork the repository**

   ```bash
   # On GitHub, click "Fork" button
   ```

2. **Clone your fork**

   ```bash
   git clone https://github.com/YOUR_USERNAME/EarthTool.git
   cd EarthTool
   ```

3. **Add upstream remote**

   ```bash
   git remote add upstream https://github.com/Arkezar/EarthTool.git
   ```

4. **Restore dependencies**

   ```bash
   dotnet restore
   ```

5. **Build the project**

   ```bash
   dotnet build
   ```

6. **Run tests**

   ```bash
   dotnet test
   ```

### IDE Setup

#### Visual Studio 2022

1. Open `EarthTool.sln`
2. Set `EarthTool.CLI` or `EarthTool.WD.GUI` as startup project
3. Build → Build Solution (F6)

#### VS Code

1. Open folder in VS Code
2. Install recommended extensions (C#, C# Dev Kit)
3. Use Terminal → Run Build Task (Ctrl+Shift+B)

#### JetBrains Rider

1. Open `EarthTool.sln`
2. Build → Build Solution
3. Run configurations are in `.run/` folder

## Coding Guidelines

We follow strict coding standards. Please read [`AGENTS.md`](AGENTS.md) for complete guidelines.

### Key Points

#### Code Style

- **Indentation**: 2 spaces (NOT tabs)
- **Line endings**: LF only
- **Encoding**: UTF-8 with BOM
- **Braces**: Always use braces, new line before open brace

#### Naming Conventions

```csharp
// Private fields: _camelCase
private readonly ILogger _logger;
private int _count;

// Properties/Methods: PascalCase
public string FileName { get; set; }
public void ProcessArchive() { }

// Interfaces: I + PascalCase
public interface IArchive { }
public interface IArchiver { }

// Constants: PascalCase
private const int MaxSize = 1024;
```

#### Null Handling

```csharp
// ✅ Use null-coalescing and null-propagation
var value = _field ?? defaultValue;
var length = _collection?.Count ?? 0;

// ✅ Enable nullable reference types
#nullable enable

// ❌ Avoid null checks
if (_field == null)
    _field = defaultValue;
```

#### Patterns

```csharp
// ✅ Use pattern matching
if (obj is ArchiveItem item)
{
    // Use item
}

// ❌ Avoid as + null check
var item = obj as ArchiveItem;
if (item != null)
{
    // Use item
}
```

### Code Formatting

Run before committing:

```bash
dotnet format EarthTool.sln --verify-no-changes
```

If there are changes:

```bash
dotnet format EarthTool.sln
```

## Commit Guidelines

We use **Conventional Commits** for clear, structured commit history.

### Format

```
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `perf`: Performance improvement
- `refactor`: Code refactoring (no behavior change)
- `docs`: Documentation changes
- `test`: Test additions or changes
- `build`: Build system changes
- `ci`: CI/CD changes
- `chore`: Maintenance tasks

### Scopes

- `cli`: CLI application
- `gui`: GUI application
- `wd`: WD archive module
- `msh`: MSH model module
- `dae`: DAE export module
- `par`: PAR parameter module
- `tex`: TEX texture module
- `common`: Shared/common code

### Examples

**Good commits:**

```bash
feat(cli): add batch processing for extract command
fix(gui): resolve memory leak in archive loading
perf(wd): optimize compression by 30%
docs: update installation guide for macOS
test(par): add tests for entity serialization
refactor(common): extract base reader class
```

**Bad commits:**

```bash
Update stuff                    # No type, vague
Fixed bug                       # No scope, not descriptive
Added new feature to CLI        # Past tense, no scope
```

### Breaking Changes

If your change breaks backward compatibility:

```bash
feat(api)!: redesign archive interface

BREAKING CHANGE: IArchive methods are now async.

Migration guide:
- Add await before archive operations
- Change return types to Task<T>
```

See [Conventional Commits Guide](docs/ci-cd/conventional-commits.md) for details.

## Pull Request Process

### Before Creating a PR

1. **Update your branch**

   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Run tests**

   ```bash
   dotnet test
   ```

3. **Format code**

   ```bash
   dotnet format
   ```

4. **Update documentation** if needed

### Creating the PR

1. **Push to your fork**

   ```bash
   git push origin your-branch-name
   ```

2. **Create PR on GitHub**
   - Use conventional commit format for title
   - Fill out the PR template
   - Link related issues

3. **PR Title Format**

   ```
   feat(cli): add support for wildcards in extract command
   fix(gui): prevent crash when opening corrupted archives
   docs: improve quick start guide
   ```

### PR Template

```markdown
## Description
Brief description of changes.

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update
- [ ] Performance improvement
- [ ] Refactoring

## Testing
- [ ] All existing tests pass
- [ ] New tests added (if applicable)
- [ ] Manual testing performed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex code
- [ ] Documentation updated
- [ ] No new warnings generated
- [ ] Conventional commit format used

## Related Issues
Closes #123
Relates to #456
```

### Review Process

1. **Automated checks** must pass:
   - Build succeeds
   - All tests pass
   - Code format is correct
   - No security vulnerabilities

2. **Code review** by maintainers:
   - Code quality and style
   - Test coverage
   - Documentation
   - Performance implications

3. **Address feedback**:
   - Make requested changes
   - Push to your branch (PR updates automatically)
   - Respond to comments

4. **Merge**:
   - Maintainer will merge once approved
   - Squash and merge for clean history

## Testing Requirements

### Test Coverage

- **Minimum**: 80% line coverage
- **Target**: 90%+ line coverage
- **New code**: Should have tests

### Writing Tests

**Structure**:

```csharp
public class FeatureTests
{
    [Fact]
    public void Method_Scenario_ExpectedResult()
    {
        // Arrange
        var input = CreateTestData();
        
        // Act
        var result = MethodUnderTest(input);
        
        // Assert
        result.Should().BeExpectedValue();
    }
}
```

**Guidelines**:

- Use `xUnit` test framework
- Use `FluentAssertions` for assertions
- Follow AAA pattern (Arrange-Act-Assert)
- Test happy path, edge cases, and error conditions
- Use descriptive test names

**Run tests**:

```bash
# All tests
dotnet test

# Specific project
dotnet test EarthTool.WD.Tests/

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Documentation

See [Testing Guide](docs/development/testing.md) for:

- Test patterns and best practices
- How to write effective tests
- Mocking strategies
- Test data generation

## Documentation

### When to Update Documentation

- **New features**: Add to user guide and API docs
- **Breaking changes**: Update migration guide
- **Configuration changes**: Update installation guide
- **Bug fixes**: Add to troubleshooting if relevant

### Documentation Types

| Type | Location | When to Update |
|------|----------|----------------|
| User guides | `docs/*.md` | New features, behavior changes |
| API docs | `docs/api/` | Public API changes |
| Architecture | `docs/architecture.md` | Design changes |
| Format specs | `docs/*_FORMAT.md` | Format discoveries |
| Code comments | In source files | Complex logic, public APIs |

### Documentation Style

- **Clear and concise**: Short sentences, simple language
- **Examples**: Include code examples where helpful
- **Structure**: Use headings, lists, tables
- **Links**: Cross-reference related documentation
- **Screenshots**: Add for GUI features (if relevant)

## Recognition

Contributors will be:

- Listed in release notes
- Credited in commit history
- Mentioned in project README (for significant contributions)

## Questions?

- **Documentation**: Check [docs/](docs/)
- **Discussion**: Open a [GitHub Discussion](https://github.com/Arkezar/EarthTool/discussions)
- **Chat**: Join Inside Earth Discord (community)

## Thank You

Your contributions make EarthTool better for everyone. Whether it's code, documentation, bug reports, or feedback - we appreciate your help! 🎉

---

**Ready to contribute?**

1. Review [AGENTS.md](AGENTS.md) for detailed guidelines
2. Check [good first issues](https://github.com/Arkezar/EarthTool/labels/good%20first%20issue)
3. Read the [Development Guide](docs/development/README.md)
