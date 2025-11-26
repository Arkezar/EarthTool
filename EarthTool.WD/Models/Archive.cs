using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace EarthTool.WD.Models
{
  public class Archive : SortedSet<IArchiveItem>, IArchive
  {
    private readonly MemoryMappedFile _memoryMappedFile;
    private bool _disposed;
    private readonly bool _timestampLocked;

    public Archive(IEarthInfo header)
      : this(header, DateTime.Now, false)
    {
    }

    private Archive(IEarthInfo header, DateTime lastModification, bool lockTimestamp)
      : this(header, lastModification, [], lockTimestamp)
    {
    }

    public Archive(IEarthInfo header, DateTime lastModification, IEnumerable<IArchiveItem> items)
      : this(header, lastModification, items, false)
    {
    }

    public Archive(IEarthInfo header, DateTime lastModification, IEnumerable<IArchiveItem> items, bool lockTimestamp)
      : base(items)
    {
      Header = header;
      LastModification = lastModification;
      _timestampLocked = lockTimestamp;
    }

    public Archive(
      IEarthInfo header,
      DateTime lastModification,
      IEnumerable<IArchiveItem> items,
      MemoryMappedFile memoryMappedFile)
      : this(header, lastModification, items, false)
    {
      _memoryMappedFile = memoryMappedFile;
    }

    public IEarthInfo Header { get; }
    public DateTime LastModification { get; private set; }
    public IReadOnlyCollection<IArchiveItem> Items => this;

    public void SetTimestamp(DateTime timestamp)
    {
      LastModification = timestamp;
    }

    public void AddItem(IArchiveItem item)
    {
      Add(item);
      if (!_timestampLocked)
      {
        LastModification = DateTime.Now;
      }
    }

    public void RemoveItem(IArchiveItem item)
    {
      Remove(item);
      if (!_timestampLocked)
      {
        LastModification = DateTime.Now;
      }
    }

    public byte[] ToByteArray(ICompressor compressor, Encoding encoding)
    {
      using var archiveStream = new MemoryStream();
      using var writer = new BinaryWriter(archiveStream, encoding, leaveOpen: true);

      // 1. Write compressed header
      var headerBytes = Header.ToByteArray(encoding);
      var compressedHeader = compressor.Compress(headerBytes);
      writer.Write(compressedHeader);

      // 2. Write all item data and track their positions
      var itemMetadata = new List<(IArchiveItem Item, int Offset, int Length)>();

      foreach (var item in Items)
      {
        var offset = (int)archiveStream.Position;
        var itemData = item.Data.ToArray();
        writer.Write(itemData);
        var length = itemData.Length;

        itemMetadata.Add((item, offset, length));
      }

      // 3. Build central directory
      using var centralDirStream = new MemoryStream();
      using var centralDirWriter = new BinaryWriter(centralDirStream, encoding);

      // Write last modification time
      centralDirWriter.Write(LastModification.ToFileTimeUtc());

      // Write item count
      centralDirWriter.Write((short)Items.Count);

      // Write each item's metadata
      foreach (var (item, offset, length) in itemMetadata)
      {
        centralDirWriter.Write(item.FileName);
        centralDirWriter.Write((byte)item.Header.Flags);
        centralDirWriter.Write(offset);
        centralDirWriter.Write(length);
        centralDirWriter.Write(item.DecompressedSize);

        // Optional fields based on flags
        if (item.Header.Flags.HasFlag(FileFlags.Named) && !string.IsNullOrEmpty(item.Header.TranslationId))
        {
          centralDirWriter.Write(item.Header.TranslationId);
        }

        if (item.Header.Flags.HasFlag(FileFlags.Resource) && item.Header.ResourceType.HasValue)
        {
          centralDirWriter.Write((int)item.Header.ResourceType.Value);
        }

        if (item.Header.Flags.HasFlag(FileFlags.Guid) && item.Header.Guid.HasValue)
        {
          centralDirWriter.Write(item.Header.Guid.Value.ToByteArray());
        }
      }

      // 4. Compress and write central directory
      var centralDirBytes = centralDirStream.ToArray();
      var compressedCentralDir = compressor.Compress(centralDirBytes);
      writer.Write(compressedCentralDir);

      // 5. Write descriptor length (compressed central directory size + 4 bytes for the length itself)
      var descriptorLength = compressedCentralDir.Length + 4;
      writer.Write(descriptorLength);

      return archiveStream.ToArray();
    }

    /// <summary>
    /// Disposes the memory-mapped file and all archive items.
    /// </summary>
    public void Dispose()
    {
      if (_disposed)
      {
        return;
      }

      // Dispose all items first (they may reference the MMF)
      foreach (var item in this)
      {
        item?.Dispose();
      }

      // Then dispose the shared MMF
      _memoryMappedFile?.Dispose();
      _disposed = true;

      GC.SuppressFinalize(this);
    }
  }
}