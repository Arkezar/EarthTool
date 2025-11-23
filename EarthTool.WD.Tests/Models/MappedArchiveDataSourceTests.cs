using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using EarthTool.WD.Models;

namespace EarthTool.WD.Tests.Models;

public class MappedArchiveDataSourceTests : IDisposable
{
    private readonly string _tempFilePath;
    private MemoryMappedFile? _mmf;

    public MappedArchiveDataSourceTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.dat");
    }

    public void Dispose()
    {
        _mmf?.Dispose();
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var testData = TestDataGenerator.GenerateSampleData(1024);
        File.WriteAllBytes(_tempFilePath, testData);
        _mmf = MemoryMappedFile.CreateFromFile(_tempFilePath, FileMode.Open);

        // Act
        var dataSource = new MappedArchiveDataSource(_mmf, 0, testData.Length);

        // Assert
        dataSource.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullFile_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new MappedArchiveDataSource(null!, 0, 100);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("file");
    }

    [Fact]
    public void Data_LazyLoads_OnFirstAccess()
    {
        // Arrange
        var testData = TestDataGenerator.GenerateSampleData(512);
        File.WriteAllBytes(_tempFilePath, testData);
        _mmf = MemoryMappedFile.CreateFromFile(_tempFilePath, FileMode.Open);
        var dataSource = new MappedArchiveDataSource(_mmf, 0, testData.Length);

        // Act - first access triggers loading
        var result = dataSource.Data;

        // Assert
        result.ToArray().Should().Equal(testData);
    }

    [Fact]
    public void Data_CachesOnMultipleAccess()
    {
        // Arrange
        var testData = TestDataGenerator.GenerateSampleData(256);
        File.WriteAllBytes(_tempFilePath, testData);
        _mmf = MemoryMappedFile.CreateFromFile(_tempFilePath, FileMode.Open);
        var dataSource = new MappedArchiveDataSource(_mmf, 0, testData.Length);

        // Act - multiple accesses
        var firstAccess = dataSource.Data;
        var secondAccess = dataSource.Data;

        // Assert - should return cached data (backed by same array)
        firstAccess.ToArray().Should().Equal(secondAccess.ToArray());
        firstAccess.ToArray().Should().Equal(testData);
    }

    [Fact]
    public void Data_WithOffset_ReadsCorrectData()
    {
        // Arrange
        var fullData = TestDataGenerator.GenerateSampleData(1000);
        File.WriteAllBytes(_tempFilePath, fullData);
        _mmf = MemoryMappedFile.CreateFromFile(_tempFilePath, FileMode.Open);
        var offset = 100;
        var length = 200;
        var expectedData = fullData[offset..(offset + length)];

        // Act
        var dataSource = new MappedArchiveDataSource(_mmf, offset, length);
        var result = dataSource.Data;

        // Assert
        result.ToArray().Should().Equal(expectedData);
    }

    [Fact]
    public void Data_WithZeroLength_ReturnsEmptyData()
    {
        // Arrange
        var testData = TestDataGenerator.GenerateSampleData(100);
        File.WriteAllBytes(_tempFilePath, testData);
        _mmf = MemoryMappedFile.CreateFromFile(_tempFilePath, FileMode.Open);

        // Act
        var dataSource = new MappedArchiveDataSource(_mmf, 0, 0);
        var result = dataSource.Data;

        // Assert
        result.Length.Should().Be(0);
    }

    [Fact]
    public void Dispose_DoesNotDisposeMemoryMappedFile()
    {
        // Arrange
        var testData = TestDataGenerator.GenerateSampleData(100);
        File.WriteAllBytes(_tempFilePath, testData);
        _mmf = MemoryMappedFile.CreateFromFile(_tempFilePath, FileMode.Open);
        var dataSource = new MappedArchiveDataSource(_mmf, 0, testData.Length);

        // Act
        dataSource.Dispose();

        // Assert - MMF should still be usable
        Action act = () =>
        {
            using var accessor = _mmf.CreateViewAccessor(0, testData.Length);
            accessor.ReadByte(0);
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var testData = TestDataGenerator.GenerateSampleData(50);
        File.WriteAllBytes(_tempFilePath, testData);
        _mmf = MemoryMappedFile.CreateFromFile(_tempFilePath, FileMode.Open);
        var dataSource = new MappedArchiveDataSource(_mmf, 0, testData.Length);

        // Act & Assert
        dataSource.Dispose();
        dataSource.Dispose();
        dataSource.Dispose();
    }

    [Fact]
    public void Data_AfterDispose_StillAccessible()
    {
        // Arrange
        var testData = TestDataGenerator.GenerateSampleData(100);
        File.WriteAllBytes(_tempFilePath, testData);
        _mmf = MemoryMappedFile.CreateFromFile(_tempFilePath, FileMode.Open);
        var dataSource = new MappedArchiveDataSource(_mmf, 0, testData.Length);

        // Act - access before dispose to trigger caching
        var dataBefore = dataSource.Data;
        dataSource.Dispose();
        var dataAfter = dataSource.Data;

        // Assert - cached data should still be accessible
        dataAfter.ToArray().Should().Equal(testData);
        dataBefore.ToArray().Should().Equal(dataAfter.ToArray());
    }

    [Fact]
    public void Data_LargeFile_HandlesCorrectly()
    {
        // Arrange
        var largeData = TestDataGenerator.GenerateSampleData(10 * 1024 * 1024); // 10 MB
        File.WriteAllBytes(_tempFilePath, largeData);
        _mmf = MemoryMappedFile.CreateFromFile(_tempFilePath, FileMode.Open);

        // Act
        var dataSource = new MappedArchiveDataSource(_mmf, 0, largeData.Length);
        var result = dataSource.Data;

        // Assert
        result.Length.Should().Be(largeData.Length);
        result.ToArray().Should().Equal(largeData);
    }

    [Fact]
    public void Constructor_MultipleAccessors_NoMemoryLeak()
    {
        // Arrange
        var testData = TestDataGenerator.GenerateSampleData(1024);
        File.WriteAllBytes(_tempFilePath, testData);
        _mmf = MemoryMappedFile.CreateFromFile(_tempFilePath, FileMode.Open);

        // Act - create multiple data sources pointing to same MMF
        var sources = new MappedArchiveDataSource[10];
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = new MappedArchiveDataSource(_mmf, 0, testData.Length);
            _ = sources[i].Data; // Force loading
        }

        // Dispose all
        foreach (var source in sources)
        {
            source.Dispose();
        }

        // Assert - MMF should still be usable (not disposed by any data source)
        Action act = () =>
        {
            using var accessor = _mmf.CreateViewAccessor(0, testData.Length);
            accessor.ReadByte(0);
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void Data_WithOffsetAtEnd_ReadsCorrectly()
    {
        // Arrange
        var testData = TestDataGenerator.GenerateSampleData(1000);
        File.WriteAllBytes(_tempFilePath, testData);
        _mmf = MemoryMappedFile.CreateFromFile(_tempFilePath, FileMode.Open);
        var offset = 900;
        var length = 100;
        var expectedData = testData[offset..];

        // Act
        var dataSource = new MappedArchiveDataSource(_mmf, offset, length);
        var result = dataSource.Data;

        // Assert
        result.ToArray().Should().Equal(expectedData);
    }
}
