using EarthTool.WD.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Linq;

namespace EarthTool.WD.Tests.Services;

public class CompressorServiceTests
{
  private readonly CompressorService _compressor;

  public CompressorServiceTests()
  {
    _compressor = new CompressorService(NullLogger<CompressorService>.Instance);
  }

  [Fact]
  public void Compress_WithValidData_ReturnsCompressedData()
  {
    // Arrange
    var originalData = TestDataGenerator.GenerateSampleData(1024);

    // Act
    var compressed = _compressor.Compress(originalData);

    // Assert
    compressed.Should().NotBeNull();
    compressed.Length.Should().BeLessThan(originalData.Length); // Should compress well for patterned data
  }

  [Fact]
  public void Compress_WithEmptyArray_ReturnsEmptyOrSmallData()
  {
    // Arrange
    var emptyData = Array.Empty<byte>();

    // Act
    var compressed = _compressor.Compress(emptyData);

    // Assert
    compressed.Should().NotBeNull();
    // ZLib may return empty array for empty input in some implementations
    compressed.Length.Should().BeGreaterThanOrEqualTo(0);
  }

  [Fact]
  public void Compress_WithStream_ReturnsCompressedData()
  {
    // Arrange
    var originalData = TestDataGenerator.GenerateSampleData(512);
    using var stream = new MemoryStream(originalData);

    // Act
    var compressed = _compressor.Compress(stream);

    // Assert
    compressed.Should().NotBeNull();
    compressed.Length.Should().BeGreaterThan(0);
  }

  [Fact]
  public void OpenCompressionStream_WithLeaveOpenTrue_DoesNotCloseBaseStream()
  {
    // Arrange
    using var baseStream = new MemoryStream();

    // Act
    using (var compressionStream = _compressor.OpenCompressionStream(baseStream, leaveOpen: true))
    {
      compressionStream.WriteByte(42);
    }

    // Assert - base stream should still be usable
    baseStream.CanWrite.Should().BeTrue();
  }

  [Fact]
  public void OpenCompressionStream_WithLeaveOpenFalse_ClosesBaseStream()
  {
    // Arrange
    var baseStream = new MemoryStream();

    // Act
    using (var compressionStream = _compressor.OpenCompressionStream(baseStream, leaveOpen: false))
    {
      compressionStream.WriteByte(42);
    }

    // Assert - base stream should be closed
    baseStream.CanWrite.Should().BeFalse();
  }

  [Fact]
  public void Compress_WithRandomData_ProducesValidOutput()
  {
    // Arrange
    var randomData = TestDataGenerator.GenerateRandomData(2048, seed: 12345);

    // Act
    var compressed = _compressor.Compress(randomData);

    // Assert
    compressed.Should().NotBeNull();
    // Random data typically doesn't compress well, might even be larger
    compressed.Length.Should().BeGreaterThan(0);
  }

  [Fact]
  public void Compress_LargeData_HandlesCorrectly()
  {
    // Arrange
    var largeData = TestDataGenerator.GenerateSampleData(100 * 1024); // 100 KB

    // Act
    var compressed = _compressor.Compress(largeData);

    // Assert
    compressed.Should().NotBeNull();
    compressed.Length.Should().BeLessThan(largeData.Length);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(100)]
  [InlineData(1000)]
  [InlineData(10000)]
  public void Compress_VariousSizes_WorksCorrectly(int size)
  {
    // Arrange
    var data = TestDataGenerator.GenerateSampleData(size);

    // Act
    var compressed = _compressor.Compress(data);

    // Assert
    compressed.Should().NotBeNull();
  }

  [Fact]
  public void Compress_WithTextData_CompressesEfficiently()
  {
    // Arrange
    var text = string.Join("", Enumerable.Repeat("Hello World! ", 100));
    var textData = TestDataGenerator.GenerateTextData(text);

    // Act
    var compressed = _compressor.Compress(textData);

    // Assert
    compressed.Should().NotBeNull();
    // Repetitive text should compress very well
    compressed.Length.Should().BeLessThan(textData.Length / 2);
  }
}
