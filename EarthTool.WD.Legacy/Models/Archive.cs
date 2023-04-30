using EarthTool.Common.Enums;
using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;

namespace EarthTool.WD.Legacy.Models
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

    public Archive(string filePath, Stream stream, IDecompressor decompressor, ICompressor compressor) : this(decompressor, compressor)
    {
      FilePath = filePath;

      Header = GetHeader(stream);
      if (Header.IsValid())
      {
        CentralDirectory = GetCentralDirectory(stream);
      }
    }

    private IArchiveCentralDirectory GetCentralDirectory(Stream stream)
    {
      stream.Seek(-8, SeekOrigin.End);
      var descriptorLength = BitConverter.ToInt32(stream.ReadBytes(4));
      stream.Seek(-descriptorLength, SeekOrigin.End);
      using (var decompressedStream = new MemoryStream(_decompressor.Decompress(stream)))
      {
        return new ArchiveCentralDirectory(decompressedStream);
      }
    }

    private IArchiveHeader GetHeader(Stream stream)
    {
      stream.Seek(-4, SeekOrigin.End);
      return new ArchiveHeader(stream);
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
