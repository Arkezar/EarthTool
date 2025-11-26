using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.WD.Models;
using System;
using System.Linq;
using System.Text;

namespace EarthTool.WD.Tests;

/// <summary>
/// Helper class for generating test data for archive testing.
/// </summary>
public static class TestDataGenerator
{
  /// <summary>
  /// Generates sample uncompressed data for testing.
  /// </summary>
  public static byte[] GenerateSampleData(int size = 1024)
  {
    var data = new byte[size];
    for (int i = 0; i < size; i++)
    {
      data[i] = (byte)(i % 256);
    }
    return data;
  }

  /// <summary>
  /// Generates random data for testing compression.
  /// </summary>
  public static byte[] GenerateRandomData(int size = 1024, int? seed = null)
  {
    var random = seed.HasValue ? new Random(seed.Value) : new Random();
    var data = new byte[size];
    random.NextBytes(data);
    return data;
  }

  /// <summary>
  /// Generates a simple text content for testing.
  /// </summary>
  public static byte[] GenerateTextData(string text)
  {
    return Encoding.UTF8.GetBytes(text);
  }

  /// <summary>
  /// Creates a mock EarthInfo header with specified flags.
  /// </summary>
  public static IEarthInfo CreateMockHeader(
      IEarthInfoFactory factory,
      FileFlags flags = FileFlags.None,
      Guid? guid = null,
      ResourceType? resourceType = null,
      string? translationId = null)
  {
    return factory.Get(flags, guid, resourceType, translationId);
  }

  /// <summary>
  /// Creates an ArchiveItem with in-memory data source.
  /// </summary>
  public static ArchiveItem CreateArchiveItem(
      string fileName,
      IEarthInfo header,
      byte[] data,
      bool isCompressed = false)
  {
    var dataSource = new InMemoryArchiveDataSource(data);
    var compressedSize = data.Length;
    var decompressedSize = data.Length;

    return new ArchiveItem(fileName, header, dataSource, compressedSize, decompressedSize);
  }

  /// <summary>
  /// Creates multiple archive items for bulk testing.
  /// </summary>
  public static ArchiveItem[] CreateMultipleArchiveItems(
      IEarthInfoFactory factory,
      int count = 5,
      int dataSize = 100)
  {
    return Enumerable.Range(1, count)
        .Select(i =>
        {
          var fileName = $"file{i}.txt";
          var data = GenerateSampleData(dataSize + i * 10);
          var header = CreateMockHeader(factory);
          return CreateArchiveItem(fileName, header, data);
        })
        .ToArray();
  }

  /// <summary>
  /// Creates a sample archive with multiple items.
  /// </summary>
  public static Archive CreateSampleArchive(
      IEarthInfoFactory factory,
      int itemCount = 3)
  {
    var archiveHeader = factory.Get(
        FileFlags.Compressed | FileFlags.Resource | FileFlags.Guid,
        Guid.NewGuid(),
        ResourceType.WdArchive);

    var archive = new Archive(archiveHeader);
    var items = CreateMultipleArchiveItems(factory, itemCount);

    foreach (var item in items)
    {
      archive.AddItem(item);
    }

    return archive;
  }
}