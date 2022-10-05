using EarthTool.Common.Enums;
using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.WD.Resources
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
        TranslationId = Flags.HasFlag(FileFlags.Named) ? br.ReadString() : null;
        ResourceType = Flags.HasFlag(FileFlags.Resource) ? (ResourceType)br.ReadInt32() : null;
        Guid = Flags.HasFlag(FileFlags.Guid) ? new Guid(br.ReadBytes(16)) : null;
      }
    }

    public ArchiveFileHeader(string fileName, int offset, int length, int decompressedLength, FileFlags flags, string translationId = null, ResourceType? resourceType = null, Guid? guid = null)
    {
      FileName = fileName;
      Offset = offset;
      Length = length;
      DecompressedLength = decompressedLength;
      TranslationId = translationId;
      ResourceType = resourceType;
      Guid = guid;
      Flags = GetFileFlags(flags);
    }

    public void SetOffset(int offset)
    {
      Offset = offset;
    }

    public byte[] ToByteArray()
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, Encoding.GetEncoding("ISO-8859-2")))
        {
          bw.Write(FileName);
          bw.Write((byte)Flags);
          bw.Write(Offset);
          bw.Write(Length);
          bw.Write(DecompressedLength);
          if (Flags.HasFlag(FileFlags.Named))
          {
            bw.Write(TranslationId);
          }
          if (Flags.HasFlag(FileFlags.Resource))
          {
            bw.Write((int)ResourceType);
          }
          if (Flags.HasFlag(FileFlags.Guid))
          {
            bw.Write(Guid.Value.ToByteArray());
          }
        }
        return output.ToArray();
      }
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