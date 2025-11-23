using System;
using System.IO;
using EarthTool.Common.Enums;
using EarthTool.WD.Models;

namespace EarthTool.WD.Tests.Factories;

public class ArchiveFactoryTests : ArchiveTestsBase
{
    [Fact]
    public void NewArchive_CreatesValidArchive()
    {
        // Act
        var archive = ArchiveFactory.NewArchive();

        // Assert
        archive.Should().NotBeNull();
        archive.Header.Should().NotBeNull();
        archive.Header.Flags.Should().HaveFlag(FileFlags.Compressed);
        archive.Header.Flags.Should().HaveFlag(FileFlags.Resource);
        archive.Header.Flags.Should().HaveFlag(FileFlags.Guid);
        archive.Header.Guid.Should().NotBeNull();
        archive.Header.ResourceType.Should().Be(ResourceType.WdArchive);
        archive.Items.Should().BeEmpty();
        archive.LastModification.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void NewArchive_WithLastModification_SetsTimestamp()
    {
        // Arrange
        var timestamp = new DateTime(2023, 5, 10, 12, 0, 0);

        // Act
        var archive = ArchiveFactory.NewArchive(timestamp);

        // Assert
        archive.Should().NotBeNull();
        archive.LastModification.Should().Be(timestamp);
    }

    [Fact]
    public void NewArchive_WithGuid_SetsGuidCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var timestamp = DateTime.Now;

        // Act
        var archive = ArchiveFactory.NewArchive(timestamp, guid);

        // Assert
        archive.Should().NotBeNull();
        archive.Header.Guid.Should().Be(guid);
        archive.LastModification.Should().Be(timestamp);
    }

    [Fact]
    public void NewArchive_GeneratesUniqueGuids()
    {
        // Act
        var archive1 = ArchiveFactory.NewArchive();
        var archive2 = ArchiveFactory.NewArchive();

        // Assert
        archive1.Header.Guid.Value.Should().NotBe(archive2.Header.Guid.Value);
    }

    [Fact]
    public void OpenArchive_WithValidFile_ReturnsArchive()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.wd");
        try
        {
            var archive = TestDataGenerator.CreateSampleArchive(EarthInfoFactory, itemCount: 2);
            var bytes = archive.ToByteArray(Compressor, Encoding);
            File.WriteAllBytes(tempFile, bytes);

            // Act
            using var loadedArchive = ArchiveFactory.OpenArchive(tempFile);

            // Assert
            loadedArchive.Should().NotBeNull();
            loadedArchive.Items.Count.Should().Be(2);
            loadedArchive.Header.ResourceType.Should().Be(ResourceType.WdArchive);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenArchive_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.wd");

        // Act
        Action act = () => ArchiveFactory.OpenArchive(nonExistentPath);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void OpenArchive_WithInvalidFormat_ThrowsException()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid()}.wd");
        try
        {
            // Write invalid data
            File.WriteAllBytes(tempFile, new byte[] { 1, 2, 3, 4, 5 });

            // Act
            Action act = () =>
            {
                using var archive = ArchiveFactory.OpenArchive(tempFile);
            };

            // Assert
            act.Should().Throw<Exception>(); // Will throw on decompression or parsing
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenArchive_WithWrongResourceType_ThrowsNotSupportedException()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"wrongtype_{Guid.NewGuid()}.wd");
        try
        {
            // Create an archive with wrong resource type
            var header = EarthInfoFactory.Get(
                FileFlags.Compressed | FileFlags.Resource,
                null,
                ResourceType.Parameters); // Wrong type!

            var archive = new Archive(header);
            var bytes = archive.ToByteArray(Compressor, Encoding);
            File.WriteAllBytes(tempFile, bytes);

            // Act
            Action act = () =>
            {
                using var loadedArchive = ArchiveFactory.OpenArchive(tempFile);
            };

            // Assert
            act.Should().Throw<NotSupportedException>()
                .WithMessage("*archive type*");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenArchive_WithEmptyArchive_ReturnsEmptyArchive()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid()}.wd");
        try
        {
            var archive = ArchiveFactory.NewArchive();
            var bytes = archive.ToByteArray(Compressor, Encoding);
            File.WriteAllBytes(tempFile, bytes);

            // Act
            using var loadedArchive = ArchiveFactory.OpenArchive(tempFile);

            // Assert
            loadedArchive.Should().NotBeNull();
            loadedArchive.Items.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenArchive_PreservesTimestamp()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"timestamp_{Guid.NewGuid()}.wd");
        var originalTimestamp = new DateTime(2023, 3, 15, 10, 30, 45);
        try
        {
            var archive = ArchiveFactory.NewArchive(originalTimestamp);
            var bytes = archive.ToByteArray(Compressor, Encoding);
            File.WriteAllBytes(tempFile, bytes);

            // Act
            using var loadedArchive = ArchiveFactory.OpenArchive(tempFile);

            // Assert
            loadedArchive.LastModification.Should().Be(originalTimestamp);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenArchive_WithLargeArchive_HandlesCorrectly()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"large_{Guid.NewGuid()}.wd");
        try
        {
            var archive = TestDataGenerator.CreateSampleArchive(EarthInfoFactory, itemCount: 50);
            var bytes = archive.ToByteArray(Compressor, Encoding);
            File.WriteAllBytes(tempFile, bytes);

            // Act
            using var loadedArchive = ArchiveFactory.OpenArchive(tempFile);

            // Assert
            loadedArchive.Items.Count.Should().Be(50);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenArchive_DisposesMemoryMappedFile()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"dispose_{Guid.NewGuid()}.wd");
        try
        {
            var archive = TestDataGenerator.CreateSampleArchive(EarthInfoFactory, itemCount: 1);
            var bytes = archive.ToByteArray(Compressor, Encoding);
            File.WriteAllBytes(tempFile, bytes);

            // Act
            using (var loadedArchive = ArchiveFactory.OpenArchive(tempFile))
            {
                // Use archive
                _ = loadedArchive.Items.Count;
            }

            // Assert - file should be accessible after disposal
            File.ReadAllBytes(tempFile).Should().NotBeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
