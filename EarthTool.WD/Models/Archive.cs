using EarthTool.Common.Enums;
using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;
using EarthTool.WD.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.WD.Resources
{
  public class Archive : IArchive
  {
    private const int CompressedHeaderLength = 33;

    private readonly IDecompressor _decompressor;
    private readonly ICompressor _compressor;

    public IArchiveHeader Header { get; }

    public IArchiveCentralDirectory CentralDirectory { get; }

    public string FilePath { get; }

    private Archive(IDecompressor decompressor, ICompressor compressor)
    {
      _decompressor = decompressor;
      _compressor = compressor;
    }

    public Archive(string filePath, Stream stream, IDecompressor decompressor, ICompressor compressor, Encoding encoding) : this(decompressor, compressor)
    {
      FilePath = filePath;

      Header = GetHeader(stream);
      if (Header.IsValid())
      {
        CentralDirectory = GetCentralDirectory(stream, encoding);
      }
    }

    private IArchiveCentralDirectory GetCentralDirectory(Stream stream, Encoding encoding)
    {
      stream.Seek(-4, SeekOrigin.End);
      var descriptorLength = BitConverter.ToInt32(stream.ReadBytes(4));
      stream.Seek(-descriptorLength, SeekOrigin.End);
      using (var decompressedStream = new MemoryStream(_decompressor.Decompress(stream)))
      {
        return new ArchiveCentralDirectory(decompressedStream, encoding);
      }
    }

    private IArchiveHeader GetHeader(Stream stream)
    {
      using (var decompressedStream = new MemoryStream(_decompressor.Decompress(stream.ReadBytes(CompressedHeaderLength))))
      {
        return new ArchiveHeader(decompressedStream);
      }
    }

    public byte[] ExtractResource(IArchiveFileHeader resourceHeader)
    {
      using (var stream = File.OpenRead(FilePath))
      {
        var rawData = resourceHeader.GetData(stream);
        return resourceHeader.Flags.HasFlag(FileFlags.Compressed) ? _decompressor.Decompress(rawData.ToArray()) : rawData.ToArray();
      }
    }

    public byte[] ToByteArray()
    {
      throw new NotImplementedException();
    }
  }
}
