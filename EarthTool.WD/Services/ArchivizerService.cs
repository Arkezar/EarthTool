using EarthTool.Common.Enums;
using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;
using EarthTool.WD;
using EarthTool.WD.Resources;
using Ionic.Zlib;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.Services
{
  public class ArchivizerService : IArchivizer
  {
    private readonly ILogger<ArchivizerService> _logger;

    public ArchivizerService(ILogger<ArchivizerService> logger)
    {
      _logger = logger;
    }

    public bool VerifyFile(string filePath)
    {
      return GetArchiveHeader(filePath).IsValid();
    }

    public IArchiveHeader GetArchiveHeader(string filePath)
    {
      using (var stream = new FileStream(filePath, FileMode.Open))
      {
        return new ArchiveHeader(Decompress(stream));
      }
    }

    public IArchive GetArchiveDescriptor(string filePath)
    {
      using (var stream = new FileStream(filePath, FileMode.Open))
      {
        stream.Seek(-4, SeekOrigin.End);
        var descriptorLength = BitConverter.ToInt32(stream.ReadBytes(4));
        stream.Seek(-descriptorLength, SeekOrigin.End);

        return new Archive(Decompress(stream));
      }
    }

    public IArchiveResource GetResourceWithData(string filePath, IArchiveResource resource)
    {
      using (var stream = new FileStream(filePath, FileMode.Open))
      {
        return GetResourceWithData(stream, resource);
      }
    }

    public IEnumerable<IArchiveResource> GetResourceWithData(string filePath, IEnumerable<IArchiveResource> resources)
    {
      using (var stream = new FileStream(filePath, FileMode.Open))
      {
        return resources.Select(resource => GetResourceWithData(stream, resource)).ToList();
      }
    }

    //create archive
    //add file

    private IArchiveResource GetResourceWithData(Stream stream, IArchiveResource resource)
    {
      stream.Seek(resource.Offset, SeekOrigin.Begin);
      var data = resource.Flags.HasFlag(FileFlags.Compressed) ? Decompress(stream) : stream.ReadBytes(resource.Length);
      return resource.SetData(data);
    }

    private byte[] Decompress(Stream compressedStream)
    {
      using (var output = new MemoryStream())
      {
        using (var decompressedData = new ZlibStream(compressedStream, CompressionMode.Decompress, true))
        {
          decompressedData.CopyTo(output);
          return output.ToArray();
        }
      }
    }

    private byte[] Compress(Stream compressedStream)
    {
      using (var output = new MemoryStream())
      {
        using (var decompressedData = new ZlibStream(compressedStream, CompressionMode.Compress, true))
        {
          decompressedData.CopyTo(output);
          return output.ToArray();
        }
      }
    }
  }
}
