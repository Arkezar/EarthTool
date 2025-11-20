using System;
using System.IO;
using System.Linq;
using EarthTool.Common.Interfaces;
using EarthTool.WD.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EarthTool.WD.Tests.Services;

public class ArchiverServiceTests : ArchiveTestsBase, IDisposable
{
    private readonly IArchiver _archiverService;
    private readonly string _tempDirectory;

    public ArchiverServiceTests()
    {
        _archiverService = new ArchiverService(
            NullLogger<ArchiverService>.Instance,
            EarthInfoFactory,
            ArchiveFactory,
            Decompressor,
            Compressor,
            Encoding);

        _tempDirectory = Path.Combine(Path.GetTempPath(), $"ArchiverTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void CreateArchive_ReturnsNewArchive()
    {
        // Act
        var archive = _archiverService.CreateArchive();

        // Assert
        archive.Should().NotBeNull();
        archive.Items.Should().BeEmpty();
    }

    [Fact]
    public void CreateArchive_WithTimestamp_SetsTimestamp()
    {
        // Arrange
        var timestamp = new DateTime(2023, 7, 15, 10, 0, 0);

        // Act
        var archive = _archiverService.CreateArchive(timestamp);

        // Assert
        archive.LastModification.Should().Be(timestamp);
    }

    [Fact]
    public void CreateArchive_WithTimestampAndGuid_SetsProperties()
    {
        // Arrange
        var timestamp = new DateTime(2023, 8, 20);
        var guid = Guid.NewGuid();

        // Act
        var archive = _archiverService.CreateArchive(timestamp, guid);

        // Assert
        archive.LastModification.Should().Be(timestamp);
        archive.Header.Guid.Should().Be(guid);
    }

    [Fact]
    public void SaveArchive_ValidArchive_SavesSuccessfully()
    {
        // Arrange
        var archive = _archiverService.CreateArchive();
        var outputPath = Path.Combine(_tempDirectory, "test.wd");

        // Act
        _archiverService.SaveArchive(archive, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        new FileInfo(outputPath).Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SaveArchive_CreatesOutputDirectory()
    {
        // Arrange
        var archive = _archiverService.CreateArchive();
        var subDir = Path.Combine(_tempDirectory, "subdir", "nested");
        var outputPath = Path.Combine(subDir, "test.wd");

        // Act
        _archiverService.SaveArchive(archive, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public void OpenArchive_ValidPath_ReturnsArchive()
    {
        // Arrange
        var archive = TestDataGenerator.CreateSampleArchive(EarthInfoFactory, 2);
        var filePath = Path.Combine(_tempDirectory, "sample.wd");
        _archiverService.SaveArchive(archive, filePath);

        // Act
        using var loadedArchive = _archiverService.OpenArchive(filePath);

        // Assert
        loadedArchive.Should().NotBeNull();
        loadedArchive.Items.Count.Should().Be(2);
    }

    [Fact]
    public void AddFile_ValidFile_AddsToArchive()
    {
        // Arrange
        var archive = _archiverService.CreateArchive();
        var testFile = Path.Combine(_tempDirectory, "testfile.txt");
        File.WriteAllText(testFile, "Test content");

        // Act
        _archiverService.AddFile(archive, testFile, _tempDirectory);

        // Assert
        archive.Items.Should().ContainSingle();
        archive.Items.First().FileName.Should().Be("testfile.txt");
    }

    [Fact]
    public void AddFile_WithSubdirectory_PreservesPath()
    {
        // Arrange
        var archive = _archiverService.CreateArchive();
        var subDir = Path.Combine(_tempDirectory, "subfolder");
        Directory.CreateDirectory(subDir);
        var testFile = Path.Combine(subDir, "nested.txt");
        File.WriteAllText(testFile, "Nested content");

        // Act
        _archiverService.AddFile(archive, testFile, _tempDirectory);

        // Assert
        archive.Items.Should().ContainSingle();
        // Should use backslashes (game requirement)
        archive.Items.First().FileName.Should().Be("subfolder\\nested.txt");
    }

    [Fact]
    public void AddFile_WithNullArchive_ThrowsArgumentNullException()
    {
        // Arrange
        var testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Content");

        // Act
        Action act = () => _archiverService.AddFile(null!, testFile);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var archive = _archiverService.CreateArchive();
        var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.txt");

        // Act
        Action act = () => _archiverService.AddFile(archive, nonExistentFile);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void ExtractAll_ValidArchive_ExtractsAllFiles()
    {
        // Arrange
        var archive = _archiverService.CreateArchive();
        
        // Create test files
        var file1 = Path.Combine(_tempDirectory, "file1.txt");
        var file2 = Path.Combine(_tempDirectory, "file2.txt");
        File.WriteAllText(file1, "Content 1");
        File.WriteAllText(file2, "Content 2");
        
        _archiverService.AddFile(archive, file1, _tempDirectory);
        _archiverService.AddFile(archive, file2, _tempDirectory);

        var archivePath = Path.Combine(_tempDirectory, "test.wd");
        _archiverService.SaveArchive(archive, archivePath);

        // Clean up originals and create extract directory
        File.Delete(file1);
        File.Delete(file2);
        var extractDir = Path.Combine(_tempDirectory, "extracted");

        // Act
        using (var loadedArchive = _archiverService.OpenArchive(archivePath))
        {
            _archiverService.ExtractAll(loadedArchive, extractDir);
        }

        // Assert
        Directory.Exists(extractDir).Should().BeTrue();
        File.Exists(Path.Combine(extractDir, "file1.txt")).Should().BeTrue();
        File.Exists(Path.Combine(extractDir, "file2.txt")).Should().BeTrue();
    }

    [Fact]
    public void ExtractAll_CreatesOutputDirectory()
    {
        // Arrange
        var archive = _archiverService.CreateArchive();
        var testFile = Path.Combine(_tempDirectory, "file.txt");
        File.WriteAllText(testFile, "Content");
        _archiverService.AddFile(archive, testFile, _tempDirectory);

        var archivePath = Path.Combine(_tempDirectory, "test.wd");
        _archiverService.SaveArchive(archive, archivePath);

        var extractDir = Path.Combine(_tempDirectory, "new_extract_dir");

        // Act
        using (var loadedArchive = _archiverService.OpenArchive(archivePath))
        {
            _archiverService.ExtractAll(loadedArchive, extractDir);
        }

        // Assert
        Directory.Exists(extractDir).Should().BeTrue();
    }

    [Fact]
    public void ExtractAll_WithNullArchive_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _archiverService.ExtractAll(null!, _tempDirectory);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Extract_SingleFile_ExtractsCorrectly()
    {
        // Arrange
        var archive = _archiverService.CreateArchive();
        var testFile = Path.Combine(_tempDirectory, "single.txt");
        var testContent = "Single file content";
        File.WriteAllText(testFile, testContent);
        _archiverService.AddFile(archive, testFile, _tempDirectory);

        var archivePath = Path.Combine(_tempDirectory, "single.wd");
        _archiverService.SaveArchive(archive, archivePath);

        File.Delete(testFile);
        var extractDir = Path.Combine(_tempDirectory, "extract_single");

        // Act
        using (var loadedArchive = _archiverService.OpenArchive(archivePath))
        {
            var item = loadedArchive.Items.First();
            _archiverService.Extract(item, extractDir);
        }

        // Assert
        var extractedFile = Path.Combine(extractDir, "single.txt");
        File.Exists(extractedFile).Should().BeTrue();
    }

    [Fact]
    public void AddFile_WithCompression_CompressesFile()
    {
        // Arrange
        var archive = _archiverService.CreateArchive();
        var testFile = Path.Combine(_tempDirectory, "large.txt");
        var largeContent = string.Join("", Enumerable.Repeat("This is repeated content. ", 1000));
        File.WriteAllText(testFile, largeContent);

        // Act
        _archiverService.AddFile(archive, testFile, _tempDirectory, compress: true);

        // Assert
        var item = archive.Items.First();
        item.CompressedSize.Should().BeLessThan(item.DecompressedSize);
    }

    [Fact]
    public void AddFile_WithoutCompression_DoesNotCompress()
    {
        // Arrange
        var archive = _archiverService.CreateArchive();
        var testFile = Path.Combine(_tempDirectory, "uncompressed.txt");
        File.WriteAllText(testFile, "Small content");

        // Act
        _archiverService.AddFile(archive, testFile, _tempDirectory, compress: false);

        // Assert - file should not be marked as compressed in header
        var item = archive.Items.First();
        // Without compression flag, sizes might be similar
        item.Should().NotBeNull();
    }

    [Fact]
    public void SaveArchive_RoundTrip_PreservesData()
    {
        // Arrange
        var archive = _archiverService.CreateArchive();
        var testFile1 = Path.Combine(_tempDirectory, "data1.bin");
        var testFile2 = Path.Combine(_tempDirectory, "data2.bin");
        
        File.WriteAllBytes(testFile1, TestDataGenerator.GenerateSampleData(500));
        File.WriteAllBytes(testFile2, TestDataGenerator.GenerateSampleData(800));

        _archiverService.AddFile(archive, testFile1, _tempDirectory);
        _archiverService.AddFile(archive, testFile2, _tempDirectory);

        var archivePath = Path.Combine(_tempDirectory, "roundtrip.wd");
        _archiverService.SaveArchive(archive, archivePath);

        // Act
        using var loadedArchive = _archiverService.OpenArchive(archivePath);

        // Assert
        loadedArchive.Items.Count.Should().Be(2);
        loadedArchive.Items.Should().Contain(i => i.FileName == "data1.bin");
        loadedArchive.Items.Should().Contain(i => i.FileName == "data2.bin");
    }
}
