using EarthTool.Common.Enums;
using EarthTool.WD.Interfaces;
using EarthTool.WD.Models;
using System;

namespace EarthTool.WD.Tests.Models;

public class ArchiveItemTests : ArchiveTestsBase
{
  [Fact]
  public void Constructor_WithValidArguments_CreatesInstance()
  {
    // Arrange
    var fileName = "test.txt";
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var data = TestDataGenerator.GenerateSampleData(100);
    var dataSource = new InMemoryArchiveDataSource(data);
    var compressedSize = 80;
    var decompressedSize = 100;

    // Act
    var item = new ArchiveItem(fileName, header, dataSource, compressedSize, decompressedSize);

    // Assert
    item.Should().NotBeNull();
    item.FileName.Should().Be(fileName);
    item.Header.Should().Be(header);
    item.CompressedSize.Should().Be(compressedSize);
    item.DecompressedSize.Should().Be(decompressedSize);
    item.Data.ToArray().Should().Equal(data);
  }

  [Fact]
  public void Constructor_WithNullFileName_CreatesInstance()
  {
    // Arrange - null is allowed for filename
    string? fileName = null;
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var data = TestDataGenerator.GenerateSampleData(50);
    var dataSource = new InMemoryArchiveDataSource(data);

    // Act
    var item = new ArchiveItem(fileName!, header, dataSource, 50, 50);

    // Assert
    item.Should().NotBeNull();
    item.FileName.Should().BeNull();
  }

  [Fact]
  public void IsCompressed_WhenFlagSet_ReturnsTrue()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(
        EarthInfoFactory,
        FileFlags.Compressed);
    var data = TestDataGenerator.GenerateSampleData(100);
    var dataSource = new InMemoryArchiveDataSource(data);
    var item = new ArchiveItem("file.dat", header, dataSource, 80, 100);

    // Act & Assert
    item.IsCompressed.Should().BeTrue();
  }

  [Fact]
  public void IsCompressed_WhenFlagNotSet_ReturnsFalse()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory, FileFlags.None);
    var data = TestDataGenerator.GenerateSampleData(100);
    var dataSource = new InMemoryArchiveDataSource(data);
    var item = new ArchiveItem("file.dat", header, dataSource, 100, 100);

    // Act & Assert
    item.IsCompressed.Should().BeFalse();
  }

  [Fact]
  public void CompareTo_SameFileName_ReturnsZero()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var data = TestDataGenerator.GenerateSampleData(100);
    var dataSource1 = new InMemoryArchiveDataSource(data);
    var dataSource2 = new InMemoryArchiveDataSource(data);
    var item1 = new ArchiveItem("file.txt", header, dataSource1, 100, 100);
    var item2 = new ArchiveItem("file.txt", header, dataSource2, 100, 100);

    // Act
    var result = item1.CompareTo(item2);

    // Assert
    result.Should().Be(0);
  }

  [Fact]
  public void CompareTo_DifferentFileName_ReturnsNonZero()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var data = TestDataGenerator.GenerateSampleData(100);
    var dataSource1 = new InMemoryArchiveDataSource(data);
    var dataSource2 = new InMemoryArchiveDataSource(data);
    var item1 = new ArchiveItem("a.txt", header, dataSource1, 100, 100);
    var item2 = new ArchiveItem("b.txt", header, dataSource2, 100, 100);

    // Act
    var result = item1.CompareTo(item2);

    // Assert
    result.Should().BeLessThan(0);
  }

  [Fact]
  public void CompareTo_CaseInsensitive_ReturnsZero()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var data = TestDataGenerator.GenerateSampleData(100);
    var dataSource1 = new InMemoryArchiveDataSource(data);
    var dataSource2 = new InMemoryArchiveDataSource(data);
    var item1 = new ArchiveItem("FILE.txt", header, dataSource1, 100, 100);
    var item2 = new ArchiveItem("file.txt", header, dataSource2, 100, 100);

    // Act
    var result = item1.CompareTo(item2);

    // Assert
    result.Should().Be(0);
  }

  [Fact]
  public void CompareTo_WithNull_ReturnsOne()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var data = TestDataGenerator.GenerateSampleData(100);
    var dataSource = new InMemoryArchiveDataSource(data);
    var item = new ArchiveItem("file.txt", header, dataSource, 100, 100);

    // Act
    var result = item.CompareTo(null);

    // Assert
    result.Should().Be(1);
  }

  [Fact]
  public void CompareTo_WithSameReference_ReturnsZero()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var data = TestDataGenerator.GenerateSampleData(100);
    var dataSource = new InMemoryArchiveDataSource(data);
    var item = new ArchiveItem("file.txt", header, dataSource, 100, 100);

    // Act
    var result = item.CompareTo(item);

    // Assert
    result.Should().Be(0);
  }

  [Fact]
  public void Dispose_DisposesDataSource()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var mockDataSource = new MockArchiveDataSource();
    var item = new ArchiveItem("file.txt", header, mockDataSource, 10, 10);

    // Act
    item.Dispose();

    // Assert
    mockDataSource.DisposeCallCount.Should().Be(1);
  }

  [Fact]
  public void Dispose_CalledMultipleTimes_DisposesOnlyOnce()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var mockDataSource = new MockArchiveDataSource();
    var item = new ArchiveItem("file.txt", header, mockDataSource, 10, 10);

    // Act
    item.Dispose();
    item.Dispose();
    item.Dispose();

    // Assert - should only dispose once
    mockDataSource.DisposeCallCount.Should().Be(1);
  }

  // Helper mock class for testing disposal
  private class MockArchiveDataSource : IArchiveDataSource
  {
    public int DisposeCallCount { get; private set; }
    public ReadOnlyMemory<byte> Data => new byte[10];

    public void Dispose()
    {
      DisposeCallCount++;
    }
  }

  [Fact]
  public void Data_AccessesDataSource()
  {
    // Arrange
    var testData = TestDataGenerator.GenerateSampleData(200);
    var dataSource = new InMemoryArchiveDataSource(testData);
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var item = new ArchiveItem("file.bin", header, dataSource, 200, 200);

    // Act
    var result = item.Data;

    // Assert
    result.ToArray().Should().Equal(testData);
  }

  [Fact]
  public void Constructor_WithZeroSizes_CreatesInstance()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var data = Array.Empty<byte>();
    var dataSource = new InMemoryArchiveDataSource(data);

    // Act
    var item = new ArchiveItem("empty.txt", header, dataSource, 0, 0);

    // Assert
    item.CompressedSize.Should().Be(0);
    item.DecompressedSize.Should().Be(0);
    item.Data.Length.Should().Be(0);
  }

  [Fact]
  public void Constructor_WithBackslashPath_PreservesPath()
  {
    // Arrange
    var fileName = "folder\\subfolder\\file.txt";
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var data = TestDataGenerator.GenerateSampleData(50);
    var dataSource = new InMemoryArchiveDataSource(data);

    // Act
    var item = new ArchiveItem(fileName, header, dataSource, 50, 50);

    // Assert
    item.FileName.Should().Be(fileName);
  }
}
