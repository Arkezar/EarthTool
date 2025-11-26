# EarthTool.WD Tests

Comprehensive unit test suite for the EarthTool.WD module (WD Archive handling).

## Test Statistics

- **Total Tests:** 118
- **Line Coverage:** 92.89%
- **Branch Coverage:** 73.68%
- **Test Framework:** xUnit with AwesomeAssertions

## Test Structure

### Models Tests (52 tests)
- **InMemoryArchiveDataSourceTests** (8 tests)
  - Constructor validation
  - Data access
  - Disposal handling
  - Edge cases (empty, large data)

- **MappedArchiveDataSourceTests** (12 tests)
  - Lazy loading verification
  - Memory-mapped file handling
  - Multiple accessor scenarios
  - Memory leak prevention
  - Offset-based reading

- **ArchiveItemTests** (14 tests)
  - Constructor validation
  - Property access
  - Comparison logic (CompareTo)
  - Disposal behavior
  - Compression flag handling

- **ArchiveTests** (18 tests)
  - Archive creation
  - Item management (Add/Remove)
  - Serialization (ToByteArray)
  - Round-trip tests
  - Timestamp handling
  - Disposal patterns

### Services Tests (38 tests)
- **CompressorServiceTests** (9 tests)
  - Compression with various data sizes
  - Stream handling
  - Empty data handling
  - Text compression efficiency

- **DecompressorServiceTests** (12 tests)
  - Decompression validation
  - Round-trip compression/decompression
  - Invalid data handling
  - Stream lifecycle management

- **ArchiverServiceTests** (17 tests)
  - Archive creation and opening
  - File addition to archives
  - Extraction operations
  - Path handling
  - Compression options
  - Error scenarios

### Factory Tests (12 tests)
- **ArchiveFactoryTests** (12 tests)
  - Archive creation with various parameters
  - Archive opening from files
  - Invalid format handling
  - GUID generation
  - Timestamp preservation
  - Memory-mapped file disposal

### Integration Tests (8 tests)
- **WDExtractorTests** (8 tests)
  - Full extraction workflow
  - Directory creation
  - Multiple file handling
  - Nested path preservation
  - Large archive handling

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run with coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~CompressorServiceTests"
```

### Run tests in watch mode
```bash
dotnet watch test
```

## Test Patterns

### AAA Pattern
All tests follow the Arrange-Act-Assert pattern:
```csharp
[Fact]
public void Method_Scenario_ExpectedResult()
{
    // Arrange - Setup test data and dependencies
    var input = CreateTestData();
    
    // Act - Execute the method under test
    var result = MethodUnderTest(input);
    
    // Assert - Verify the result
    result.Should().BeExpectedValue();
}
```

### Test Naming Convention
- `Method_Scenario_ExpectedResult`
- Examples:
  - `Constructor_WithValidData_CreatesInstance`
  - `Compress_EmptyArray_ReturnsValidData`
  - `OpenArchive_NonExistentFile_ThrowsException`

## Test Helpers

### TestDataGenerator
Provides utility methods for generating test data:
- `GenerateSampleData(int size)` - Patterned data
- `GenerateRandomData(int size, int? seed)` - Random bytes
- `GenerateTextData(string text)` - UTF-8 encoded text
- `CreateMockHeader(...)` - Mock EarthInfo headers
- `CreateArchiveItem(...)` - Test archive items
- `CreateSampleArchive(...)` - Complete test archives

### ArchiveTestsBase
Base class providing:
- Pre-configured service instances (ArchiveFactory, Compressor, etc.)
- Encoding setup
- Dependency injection container
- Null loggers for tests

## Adding New Tests

1. Create test class inheriting from `ArchiveTestsBase` if needed
2. Follow naming conventions
3. Use FluentAssertions for assertions
4. Clean up resources (implement IDisposable for file operations)
5. Test happy path, edge cases, and error scenarios

Example:
```csharp
public class NewFeatureTests : ArchiveTestsBase, IDisposable
{
    private readonly string _tempDir;
    
    public NewFeatureTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
    
    [Fact]
    public void Feature_ValidInput_ReturnsExpected()
    {
        // Test implementation
    }
}
```

## Coverage Goals

- **Minimum:** 80% line coverage
- **Target:** 90%+ line coverage
- **Current:** 92.89% ✅

## Test Categories

Tests cover:
- ✅ Constructors and initialization
- ✅ Public API methods
- ✅ Property access
- ✅ Error handling and exceptions
- ✅ Edge cases (empty, null, large data)
- ✅ Resource disposal
- ✅ Round-trip operations
- ✅ Integration scenarios

## Known Limitations

1. Some integration tests require temporary file system operations
2. Memory-mapped file tests may require elevated permissions on some systems

## CI/CD Integration

Tests are designed to run in CI/CD pipelines:
- No external dependencies required
- Deterministic results (seeded random where used)
- Proper cleanup of temporary resources
- Fast execution (~50ms for full suite)

## Troubleshooting

### Tests failing with file access errors
- Ensure temporary directories are writable
- Check for antivirus interference with temp files

### Coverage not generating
```bash
dotnet add package coverlet.collector
dotnet test --collect:"XPlat Code Coverage"
```

### Tests timeout
- Increase timeout in test settings
- Check for resource leaks (undisposed streams)

## Contributing

When adding new features to EarthTool.WD:
1. Write tests first (TDD approach recommended)
2. Ensure all existing tests pass
3. Maintain or improve coverage percentage
4. Document complex test scenarios
5. Update this README if adding new test categories
