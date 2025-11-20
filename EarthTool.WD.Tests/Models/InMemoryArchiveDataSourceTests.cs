using System;
using EarthTool.WD.Models;

namespace EarthTool.WD.Tests.Models;

public class InMemoryArchiveDataSourceTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInstance()
    {
        // Arrange
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var dataSource = new InMemoryArchiveDataSource(testData);

        // Assert
        dataSource.Should().NotBeNull();
        dataSource.Data.ToArray().Should().Equal(testData);
    }

    [Fact]
    public void Constructor_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        byte[]? nullData = null;

        // Act
        Action act = () => new InMemoryArchiveDataSource(nullData!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("data");
    }

    [Fact]
    public void Data_ReturnsReadOnlyMemory()
    {
        // Arrange
        var testData = new byte[] { 10, 20, 30 };
        var dataSource = new InMemoryArchiveDataSource(testData);

        // Act
        var result = dataSource.Data;

        // Assert
        result.Length.Should().Be(testData.Length);
        result.ToArray().Should().Equal(testData);
    }

    [Fact]
    public void Data_ReturnsSameDataOnMultipleAccess()
    {
        // Arrange
        var testData = new byte[] { 100, 200 };
        var dataSource = new InMemoryArchiveDataSource(testData);

        // Act
        var firstAccess = dataSource.Data;
        var secondAccess = dataSource.Data;

        // Assert
        firstAccess.ToArray().Should().Equal(secondAccess.ToArray());
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var testData = new byte[] { 1, 2, 3 };
        var dataSource = new InMemoryArchiveDataSource(testData);

        // Act & Assert - should not throw
        dataSource.Dispose();
        dataSource.Dispose();
    }

    [Fact]
    public void Dispose_DataStillAccessibleAfterDispose()
    {
        // Arrange
        var testData = new byte[] { 5, 10, 15 };
        var dataSource = new InMemoryArchiveDataSource(testData);

        // Act
        dataSource.Dispose();
        var result = dataSource.Data;

        // Assert - data should still be accessible (no unmanaged resources)
        result.ToArray().Should().Equal(testData);
    }

    [Fact]
    public void Constructor_WithEmptyArray_CreatesInstance()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act
        var dataSource = new InMemoryArchiveDataSource(emptyData);

        // Assert
        dataSource.Should().NotBeNull();
        dataSource.Data.Length.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithLargeData_HandlesCorrectly()
    {
        // Arrange
        var largeData = new byte[1024 * 1024]; // 1 MB
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }

        // Act
        var dataSource = new InMemoryArchiveDataSource(largeData);

        // Assert
        dataSource.Data.Length.Should().Be(largeData.Length);
        dataSource.Data.ToArray().Should().Equal(largeData);
    }
}
