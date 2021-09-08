using EarthTool.Common.Enums;
using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;
using Ionic.Zlib;
using System;
using System.IO;
using System.Text;

namespace EarthTool.WD.Resources
{
  public class ArchiveResource : IArchiveResource
  {
    public string Filename { get; }

    public FileFlags Flags { get; }

    public int Offset { get; }

    public int Length { get; }

    public int DecompressedLength { get; }

    public string TranslationId { get; }

    public ResourceType? ResourceType { get; }

    public Guid? Guid { get; }

    public ArchiveResource(Stream stream)
    {
      Filename = GetName(stream);
      Flags = (FileFlags)stream.ReadByte();
      Offset = BitConverter.ToInt32(stream.ReadBytes(4));
      Length = BitConverter.ToInt32(stream.ReadBytes(4));
      DecompressedLength = BitConverter.ToInt32(stream.ReadBytes(4));
      TranslationId = Flags.HasFlag(FileFlags.Named) ? GetName(stream) : string.Empty;

      if (Flags.HasFlag(FileFlags.Resource))
      {
        ResourceType = (ResourceType)BitConverter.ToInt32(stream.ReadBytes(4));
      }

      if (Flags.HasFlag(FileFlags.Guid))
      {
        Guid = new Guid(stream.ReadBytes(16));
      }
    }

    public override string ToString()
    {
      return $"{Guid} {Filename} Flags: [{Flags}] R: {ResourceType} TranslationId: {TranslationId} Offset: {Offset} Compressed: {Length} Uncompressed: {DecompressedLength}";
    }

    public byte[] GetData(Stream stream)
    {
      return stream.ReadBytes(Length, Offset);
    }

    private string GetName(Stream stream)
    {
      return Encoding.GetEncoding("ISO-8859-2").GetString(stream.ReadBytes(stream.ReadByte()));
    }
  }
}