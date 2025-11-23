using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Validation;
using EarthTool.WD.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace EarthTool.WD.Factories
{
  public class ArchiveFactory(
    ILogger<ArchiveFactory> logger,
    IDecompressor decompressor,
    IEarthInfoFactory earthInfoFactory,
    Encoding encoding)
    : IArchiveFactory
  {
    private readonly ILogger<ArchiveFactory> _logger = logger;

    public IArchive NewArchive()
    {
      var header = earthInfoFactory.Get(FileFlags.Compressed | FileFlags.Resource | FileFlags.Guid, Guid.NewGuid(), ResourceType.WdArchive);
      return new Archive(header);
    }

    public IArchive NewArchive(DateTime lastModification)
    {
      var header = earthInfoFactory.Get(FileFlags.Compressed | FileFlags.Resource | FileFlags.Guid, Guid.NewGuid(), ResourceType.WdArchive);
      return new Archive(header, lastModification, [], lockTimestamp: true);
    }

    public IArchive NewArchive(DateTime lastModification, Guid guid)
    {
      var header = earthInfoFactory.Get(FileFlags.Compressed | FileFlags.Resource | FileFlags.Guid, guid, ResourceType.WdArchive);
      return new Archive(header, lastModification, [], lockTimestamp: true);
    }

    public IArchive OpenArchive(string path)
    {
      var validatedPath = PathValidator.ValidateFileExists(path);
      var fileInfo = new FileInfo(validatedPath);
      var fileSize = fileInfo.Length;

      // Create single memory-mapped file for entire archive
      var memoryMappedFile = OpenMemoryMappedFile(validatedPath);

      try
      {
        var header = GetArchiveHeader(memoryMappedFile);
        if (header.ResourceType != ResourceType.WdArchive)
        {
          throw new NotSupportedException($"Unsupported archive type: {header.ResourceType}");
        }

        // Read central directory from MMF (not entire file!)
        using var centralDirectoryReader = OpenCentralDirectoryReader(memoryMappedFile, fileSize);
        var lastModified = DateTime.FromFileTimeUtc(centralDirectoryReader.ReadInt64());
        var items = GetItemHandles(centralDirectoryReader, memoryMappedFile);

        return new Archive(header, lastModified, items, memoryMappedFile);
      }
      catch
      {
        memoryMappedFile?.Dispose();
        throw;
      }
    }

    private IEnumerable<IArchiveItem> GetItemHandles(BinaryReader reader, MemoryMappedFile memoryMappedFile)
    {
      var itemCount = reader.ReadInt16();

      return Enumerable.Range(0, itemCount).Select(_ =>
      {
        var filePath = reader.ReadString();
        var flags = (FileFlags)reader.ReadByte();
        var offset = reader.ReadInt32();
        var compressedSize = reader.ReadInt32();
        var decompressedSize = reader.ReadInt32();
        var translationId = flags.HasFlag(FileFlags.Named) ? reader.ReadString() : null;
        var resourceType = flags.HasFlag(FileFlags.Resource) ? (ResourceType?)reader.ReadInt32() : null;
        var guid = flags.HasFlag(FileFlags.Guid) ? (Guid?)new Guid(reader.ReadBytes(16)) : null;
        var header = earthInfoFactory.Get(flags, guid, resourceType, translationId);

        var dataSource = new MappedArchiveDataSource(memoryMappedFile, offset, compressedSize);
        return new ArchiveItem(filePath, header, dataSource, compressedSize, decompressedSize);
      }).ToImmutableArray();
    }
    
    /// <summary>
    /// Open central directory stream from memory-mapped file without loading entire archive.
    /// 
    /// Archive file structure:
    /// [Compressed Header]
    /// [Item 1 Data]
    /// [Item 2 Data]
    /// ...
    /// [Item N Data]
    /// [Compressed Central Directory] <- contains metadata for all items
    /// [Descriptor Length (4 bytes)]   <- size of (Central Directory + 4)
    /// 
    /// Caller is responsible for disposing the returned BinaryReader and its underlying streams.
    /// </summary>
    private BinaryReader OpenCentralDirectoryReader(MemoryMappedFile mmf, long fileSize)
    {
      // Read last 4 bytes to get descriptor length
      using var accessor = mmf.CreateViewAccessor(fileSize - sizeof(int), sizeof(int), MemoryMappedFileAccess.Read);
      var descriptorLength = accessor.ReadInt32(0);

      // Calculate central directory location
      // Structure: [... items ...][compressed central dir][descriptor length]
      // descriptorLength = size of compressed central dir + 4 bytes for length itself
      var centralDirOffset = fileSize       - descriptorLength;  // Skip back by full descriptor
      var centralDirSize = descriptorLength - sizeof(int);       // Exclude the 4-byte length field

      MemoryMappedViewStream centralDirStream = null;
      Stream decompressionStream = null;
      
      try
      {
        centralDirStream = mmf.CreateViewStream(
          centralDirOffset,
          centralDirSize,
          MemoryMappedFileAccess.Read);

        // Decompression stream will own centralDirStream (leaveOpen: false)
        decompressionStream = decompressor.OpenDecompressionStream(centralDirStream, leaveOpen: false);
        
        // BinaryReader will own decompressionStream
        return new BinaryReader(decompressionStream, encoding, leaveOpen: false);
      }
      catch
      {
        decompressionStream?.Dispose();
        centralDirStream?.Dispose();
        throw;
      }
    }

    private IEarthInfo GetArchiveHeader(MemoryMappedFile mmf)
    {
      using var stream = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
      using var decompressedStream = decompressor.OpenDecompressionStream(stream);
      return earthInfoFactory.Get(decompressedStream);
    }
    
    private static MemoryMappedFile OpenMemoryMappedFile(string validatedPath)
      => MemoryMappedFile.CreateFromFile(
        validatedPath,
        FileMode.Open,
        null,
        0,
        MemoryMappedFileAccess.Read);
  }
}