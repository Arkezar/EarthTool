using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
using System.Text;

namespace EarthTool.WD.Legacy.Models
{
  public class ArchiveFileHeader : IArchiveFileHeader
  {
    public string FileName { get; }

    public int Offset { get; private set; }

    public int Length { get; }

    public int DecompressedLength { get; }

    public string TranslationId { get; }

    public ResourceType? ResourceType { get; }

    public Guid? Guid { get; }

    public FileFlags Flags { get; }

    public ArchiveFileHeader(Stream stream)
    {
      using (var br = new BinaryReader(stream, Encoding.GetEncoding("ISO-8859-2"), true))
      {
        FileName = br.ReadString();
        Flags = (FileFlags)br.ReadByte();
        Offset = br.ReadInt32();
        Length = br.ReadInt32();
        DecompressedLength = br.ReadInt32();
      }
    }

    public void SetOffset(int offset)
    {
      Offset = offset;
    }

    public byte[] ToByteArray()
    {
      throw new NotSupportedException();
    }

    public ReadOnlySpan<byte> GetData(Stream stream)
    {
      stream.Seek(Offset, SeekOrigin.Begin);
      var data = new byte[Length];
      stream.Read(data);
      return data;
    }

    public IEarthInfo ToEarthInfo()
    {
      if (!string.IsNullOrEmpty(TranslationId) || Guid.HasValue || ResourceType.HasValue)
      {
        return new EarthInfo
        {
          TranslationId = TranslationId,
          Flags = Flags,
          Guid = Guid,
          ResourceType = ResourceType,
          FilePath = FileName
        };
      } else
      {
        return default;
      }
    }

    public override string ToString()
    {
      return $"{Guid} {FileName} Flags: [{Flags}] R: {ResourceType} TranslationId: {TranslationId} Offset: {Offset} Compressed: {Length} Uncompressed: {DecompressedLength}";
    }

    private FileFlags GetFileFlags(FileFlags flags)
    {
      if (TranslationId != null)
      {
        flags |= FileFlags.Named;
      }
      else
      {
        flags ^= FileFlags.Named;
      }

      if (ResourceType != null)
      {
        flags |= FileFlags.Resource;
      }
      else
      {
        flags ^= FileFlags.Resource;
      }

      if (Guid != null)
      {
        flags |= FileFlags.Guid;
      }
      else
      {
        flags ^= FileFlags.Guid;
      }
      return flags;
    }
  }
}