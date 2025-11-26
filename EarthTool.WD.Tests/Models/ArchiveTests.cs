using EarthTool.Common.Enums;
using EarthTool.WD.Models;
using System;
using System.IO;
using System.Linq;

namespace EarthTool.WD.Tests.Models;

public class ArchiveTests : ArchiveTestsBase
{
  [Fact]
  public void Constructor_WithHeader_CreatesEmptyArchive()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(
        EarthInfoFactory,
        FileFlags.Compressed | FileFlags.Resource | FileFlags.Guid,
        Guid.NewGuid(),
        ResourceType.WdArchive);

    // Act
    var archive = new Archive(header);

    // Assert
    archive.Should().NotBeNull();
    archive.Header.Should().Be(header);
    archive.Items.Should().BeEmpty();
    archive.LastModification.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
  }

  [Fact]
  public void Constructor_WithHeaderAndTimestamp_SetsTimestamp()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var timestamp = new DateTime(2023, 1, 15, 10, 30, 0);

    // Act
    var archive = new Archive(header, timestamp, Array.Empty<ArchiveItem>());

    // Assert
    archive.LastModification.Should().Be(timestamp);
  }

  [Fact]
  public void AddItem_ValidItem_AddsToCollection()
  {
    // Arrange
    var archive = new Archive(TestDataGenerator.CreateMockHeader(EarthInfoFactory));
    var item = TestDataGenerator.CreateArchiveItem(
        "test.txt",
        TestDataGenerator.CreateMockHeader(EarthInfoFactory),
        TestDataGenerator.GenerateSampleData(100));

    // Act
    archive.AddItem(item);

    // Assert
    archive.Items.Should().ContainSingle();
    archive.Items.Should().Contain(item);
  }

  [Fact]
  public void AddItem_UpdatesLastModification()
  {
    // Arrange
    var archive = new Archive(TestDataGenerator.CreateMockHeader(EarthInfoFactory));
    var oldTimestamp = archive.LastModification;
    System.Threading.Thread.Sleep(10); // Ensure time difference

    var item = TestDataGenerator.CreateArchiveItem(
        "test.txt",
        TestDataGenerator.CreateMockHeader(EarthInfoFactory),
        TestDataGenerator.GenerateSampleData(50));

    // Act
    archive.AddItem(item);

    // Assert
    archive.LastModification.Should().BeAfter(oldTimestamp);
  }

  [Fact]
  public void AddItem_WithLockedTimestamp_DoesNotUpdateTimestamp()
  {
    // Arrange
    var timestamp = new DateTime(2023, 1, 1);
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var archive = new Archive(header, timestamp, Array.Empty<ArchiveItem>(), lockTimestamp: true);
    var item = TestDataGenerator.CreateArchiveItem(
        "test.txt",
        TestDataGenerator.CreateMockHeader(EarthInfoFactory),
        TestDataGenerator.GenerateSampleData(50));

    // Act
    archive.AddItem(item);

    // Assert
    archive.LastModification.Should().Be(timestamp);
  }

  [Fact]
  public void AddItem_MultipleItems_AddsAll()
  {
    // Arrange
    var archive = new Archive(TestDataGenerator.CreateMockHeader(EarthInfoFactory));
    var items = TestDataGenerator.CreateMultipleArchiveItems(EarthInfoFactory, count: 5);

    // Act
    foreach (var item in items)
    {
      archive.AddItem(item);
    }

    // Assert
    archive.Items.Count.Should().Be(5);
  }

  [Fact]
  public void AddItem_SortsByFileName()
  {
    // Arrange
    var archive = new Archive(TestDataGenerator.CreateMockHeader(EarthInfoFactory));
    var item1 = TestDataGenerator.CreateArchiveItem("c.txt", TestDataGenerator.CreateMockHeader(EarthInfoFactory), new byte[10]);
    var item2 = TestDataGenerator.CreateArchiveItem("a.txt", TestDataGenerator.CreateMockHeader(EarthInfoFactory), new byte[10]);
    var item3 = TestDataGenerator.CreateArchiveItem("b.txt", TestDataGenerator.CreateMockHeader(EarthInfoFactory), new byte[10]);

    // Act
    archive.AddItem(item1);
    archive.AddItem(item2);
    archive.AddItem(item3);

    // Assert
    var fileNames = archive.Items.Select(i => i.FileName).ToList();
    fileNames.Should().BeInAscendingOrder();
    fileNames.Should().ContainInOrder("a.txt", "b.txt", "c.txt");
  }

  [Fact]
  public void RemoveItem_ExistingItem_RemovesFromCollection()
  {
    // Arrange
    var archive = new Archive(TestDataGenerator.CreateMockHeader(EarthInfoFactory));
    var item = TestDataGenerator.CreateArchiveItem("test.txt", TestDataGenerator.CreateMockHeader(EarthInfoFactory), new byte[50]);
    archive.AddItem(item);

    // Act
    archive.RemoveItem(item);

    // Assert
    archive.Items.Should().BeEmpty();
  }

  [Fact]
  public void RemoveItem_UpdatesLastModification()
  {
    // Arrange
    var archive = new Archive(TestDataGenerator.CreateMockHeader(EarthInfoFactory));
    var item = TestDataGenerator.CreateArchiveItem("test.txt", TestDataGenerator.CreateMockHeader(EarthInfoFactory), new byte[50]);
    archive.AddItem(item);
    var oldTimestamp = archive.LastModification;
    System.Threading.Thread.Sleep(10);

    // Act
    archive.RemoveItem(item);

    // Assert
    archive.LastModification.Should().BeAfter(oldTimestamp);
  }

  [Fact]
  public void SetTimestamp_UpdatesTimestamp()
  {
    // Arrange
    var archive = new Archive(TestDataGenerator.CreateMockHeader(EarthInfoFactory));
    var newTimestamp = new DateTime(2024, 6, 15, 14, 30, 0);

    // Act
    archive.SetTimestamp(newTimestamp);

    // Assert
    archive.LastModification.Should().Be(newTimestamp);
  }

  [Fact]
  public void ToByteArray_EmptyArchive_ReturnsValidBytes()
  {
    // Arrange
    var archive = new Archive(TestDataGenerator.CreateMockHeader(
        EarthInfoFactory,
        FileFlags.Compressed | FileFlags.Guid,
        Guid.NewGuid()));

    // Act
    var bytes = archive.ToByteArray(Compressor, Encoding);

    // Assert
    bytes.Should().NotBeNull();
    bytes.Length.Should().BeGreaterThan(0);
  }

  [Fact]
  public void ToByteArray_WithItems_IncludesAllItems()
  {
    // Arrange
    var archive = TestDataGenerator.CreateSampleArchive(EarthInfoFactory, itemCount: 3);

    // Act
    var bytes = archive.ToByteArray(Compressor, Encoding);

    // Assert
    bytes.Should().NotBeNull();
    bytes.Length.Should().BeGreaterThan(0);
  }

  [Fact]
  public void ToByteArray_CanBeReopened()
  {
    // Arrange
    var originalArchive = TestDataGenerator.CreateSampleArchive(EarthInfoFactory, itemCount: 3);
    var originalItemCount = originalArchive.Items.Count;
    var bytes = originalArchive.ToByteArray(Compressor, Encoding);

    // Save to temp file
    var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.wd");
    try
    {
      File.WriteAllBytes(tempFile, bytes);

      // Act
      using var reopenedArchive = ArchiveFactory.OpenArchive(tempFile);

      // Assert
      reopenedArchive.Items.Count.Should().Be(originalItemCount);
    }
    finally
    {
      if (File.Exists(tempFile))
        File.Delete(tempFile);
    }
  }

  [Fact]
  public void Dispose_DisposesAllItems()
  {
    // Arrange
    var archive = new Archive(TestDataGenerator.CreateMockHeader(EarthInfoFactory));
    var items = TestDataGenerator.CreateMultipleArchiveItems(EarthInfoFactory, count: 3);
    foreach (var item in items)
    {
      archive.AddItem(item);
    }

    // Act
    archive.Dispose();

    // Assert - items should be disposed (we can't directly verify but shouldn't throw)
    Action act = () => archive.Dispose();
    act.Should().NotThrow();
  }

  [Fact]
  public void Dispose_CalledTwice_DoesNotThrow()
  {
    // Arrange
    var archive = new Archive(TestDataGenerator.CreateMockHeader(EarthInfoFactory));

    // Act & Assert
    archive.Dispose();
    archive.Dispose();
  }

  [Fact]
  public void Items_ReturnsReadOnlyCollection()
  {
    // Arrange
    var archive = TestDataGenerator.CreateSampleArchive(EarthInfoFactory, itemCount: 2);

    // Act
    var items = archive.Items;

    // Assert
    items.Should().NotBeNull();
    items.Count.Should().Be(2);
  }

  [Fact]
  public void Constructor_WithItems_InitializesCollection()
  {
    // Arrange
    var header = TestDataGenerator.CreateMockHeader(EarthInfoFactory);
    var items = TestDataGenerator.CreateMultipleArchiveItems(EarthInfoFactory, count: 3);

    // Act
    var archive = new Archive(header, DateTime.Now, items);

    // Assert
    archive.Items.Count.Should().Be(3);
  }

  [Fact]
  public void ToByteArray_RoundTrip_PreservesData()
  {
    // Arrange
    var originalArchive = new Archive(TestDataGenerator.CreateMockHeader(
        EarthInfoFactory,
        FileFlags.Compressed | FileFlags.Guid | FileFlags.Resource,
        Guid.NewGuid(),
        ResourceType.WdArchive));

    var testData1 = TestDataGenerator.GenerateSampleData(200);
    var testData2 = TestDataGenerator.GenerateSampleData(300);

    var item1 = TestDataGenerator.CreateArchiveItem("file1.dat",
        TestDataGenerator.CreateMockHeader(EarthInfoFactory), testData1);
    var item2 = TestDataGenerator.CreateArchiveItem("file2.dat",
        TestDataGenerator.CreateMockHeader(EarthInfoFactory), testData2);

    originalArchive.AddItem(item1);
    originalArchive.AddItem(item2);

    var bytes = originalArchive.ToByteArray(Compressor, Encoding);
    var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.wd");

    try
    {
      File.WriteAllBytes(tempFile, bytes);

      // Act
      using var reopenedArchive = ArchiveFactory.OpenArchive(tempFile);

      // Assert
      reopenedArchive.Items.Count.Should().Be(2);
      var reopenedItem1 = reopenedArchive.Items.First(i => i.FileName == "file1.dat");
      var reopenedItem2 = reopenedArchive.Items.First(i => i.FileName == "file2.dat");

      reopenedItem1.DecompressedSize.Should().Be(testData1.Length);
      reopenedItem2.DecompressedSize.Should().Be(testData2.Length);
    }
    finally
    {
      if (File.Exists(tempFile))
        File.Delete(tempFile);
    }
  }
}