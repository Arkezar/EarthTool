using System;
using System.IO;
using EarthTool.WD.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EarthTool.WD.Tests.Services;

public class DecompressorServiceTests
{
    private readonly CompressorService _compressor;
    private readonly DecompressorService _decompressor;

    public DecompressorServiceTests()
    {
        _compressor = new CompressorService(NullLogger<CompressorService>.Instance);
        _decompressor = new DecompressorService(NullLogger<DecompressorService>.Instance);
    }

    [Fact]
    public void Decompress_WithValidCompressedData_ReturnsOriginalData()
    {
        // Arrange
        var originalData = TestDataGenerator.GenerateSampleData(1024);
        var compressed = _compressor.Compress(originalData);

        // Act
        var decompressed = _decompressor.Decompress(compressed);

        // Assert
        decompressed.Should().Equal(originalData);
    }

    [Fact]
    public void Decompress_WithByteArray_WorksCorrectly()
    {
        // Arrange
        var originalData = TestDataGenerator.GenerateSampleData(512);
        var compressed = _compressor.Compress(originalData);

        // Act
        var decompressed = _decompressor.Decompress(compressed);

        // Assert
        decompressed.Should().Equal(originalData);
    }

    [Fact]
    public void Decompress_WithReadOnlySpan_WorksCorrectly()
    {
        // Arrange
        var originalData = TestDataGenerator.GenerateSampleData(256);
        var compressed = _compressor.Compress(originalData);
        ReadOnlySpan<byte> compressedSpan = compressed;

        // Act
        var decompressed = _decompressor.Decompress(compressedSpan);

        // Assert
        decompressed.Should().Equal(originalData);
    }

    [Fact]
    public void Decompress_WithStream_WorksCorrectly()
    {
        // Arrange
        var originalData = TestDataGenerator.GenerateSampleData(800);
        var compressed = _compressor.Compress(originalData);
        using var stream = new MemoryStream(compressed);

        // Act
        var decompressed = _decompressor.Decompress(stream);

        // Assert
        decompressed.Should().Equal(originalData);
    }

    [Fact]
    public void OpenDecompressionStream_WithLeaveOpenTrue_DoesNotCloseBaseStream()
    {
        // Arrange
        var originalData = TestDataGenerator.GenerateSampleData(100);
        var compressed = _compressor.Compress(originalData);
        using var baseStream = new MemoryStream(compressed);

        // Act
        using (var decompressionStream = _decompressor.OpenDecompressionStream(baseStream, leaveOpen: true))
        {
            decompressionStream.ReadByte();
        }

        // Assert - base stream should still be usable
        baseStream.CanRead.Should().BeTrue();
    }

    [Fact]
    public void OpenDecompressionStream_WithLeaveOpenFalse_ClosesBaseStream()
    {
        // Arrange
        var originalData = TestDataGenerator.GenerateSampleData(100);
        var compressed = _compressor.Compress(originalData);
        var baseStream = new MemoryStream(compressed);

        // Act
        using (var decompressionStream = _decompressor.OpenDecompressionStream(baseStream, leaveOpen: false))
        {
            decompressionStream.ReadByte();
        }

        // Assert - base stream should be closed
        baseStream.CanRead.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void Compress_Decompress_RoundTrip_Success(int size)
    {
        // Arrange
        var originalData = TestDataGenerator.GenerateSampleData(size);

        // Act
        var compressed = _compressor.Compress(originalData);
        var decompressed = _decompressor.Decompress(compressed);

        // Assert
        decompressed.Should().Equal(originalData);
    }

    [Fact]
    public void Decompress_WithInvalidData_ThrowsException()
    {
        // Arrange
        var invalidData = new byte[] { 1, 2, 3, 4, 5 }; // Not valid ZLib data

        // Act
        Action act = () => _decompressor.Decompress(invalidData);

        // Assert
        act.Should().Throw<Exception>(); // ZLib will throw on invalid format
    }

    [Fact]
    public void Compress_Decompress_RandomData_RoundTrip()
    {
        // Arrange
        var randomData = TestDataGenerator.GenerateRandomData(2048, seed: 67890);

        // Act
        var compressed = _compressor.Compress(randomData);
        var decompressed = _decompressor.Decompress(compressed);

        // Assert
        decompressed.Should().Equal(randomData);
    }

    [Fact]
    public void Compress_Decompress_TextData_RoundTrip()
    {
        // Arrange
        var text = "This is a test string with some repetitive content. " +
                   "This is a test string with some repetitive content. " +
                   "This is a test string with some repetitive content.";
        var textData = TestDataGenerator.GenerateTextData(text);

        // Act
        var compressed = _compressor.Compress(textData);
        var decompressed = _decompressor.Decompress(compressed);

        // Assert
        decompressed.Should().Equal(textData);
    }

    [Fact]
    public void Decompress_EmptyCompressedData_ReturnsEmptyArray()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();
        var compressed = _compressor.Compress(emptyData);

        // Act & Assert
        if (compressed.Length > 0)
        {
            var decompressed = _decompressor.Decompress(compressed);
            decompressed.Should().BeEmpty();
        }
        else
        {
            // If compressor returns empty for empty input, that's also valid
            compressed.Should().BeEmpty();
        }
    }

    [Fact]
    public void Compress_Decompress_LargeData_RoundTrip()
    {
        // Arrange
        var largeData = TestDataGenerator.GenerateSampleData(500 * 1024); // 500 KB

        // Act
        var compressed = _compressor.Compress(largeData);
        var decompressed = _decompressor.Decompress(compressed);

        // Assert
        decompressed.Should().Equal(largeData);
        compressed.Length.Should().BeLessThan(largeData.Length); // Should compress well
    }
}
