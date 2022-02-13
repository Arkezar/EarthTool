using EarthTool.Common.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.Common.Models
{
  public class EarthFile
  {
    private static readonly byte[] Header = new byte[] { 0xFF, 0xA1, 0xD0 };

    public string FilePath { get; }

    public FileFlags Flags { get; }

    public string TranslationId { get; }

    public ResourceType? ResourceType { get; }

    public Guid? Guid { get; }

    public EarthFile(string filePath, Stream stream)
    {
      FilePath = filePath;

      if (IsEarthFormat(stream))
      {
        using (var br = new BinaryReader(stream, Encoding.GetEncoding("ISO-8859-2"), true))
        {
          Flags = (FileFlags)br.ReadByte();
          TranslationId = Flags.HasFlag(FileFlags.Named) ? br.ReadString() : null;
          ResourceType = Flags.HasFlag(FileFlags.Resource) ? (ResourceType)br.ReadInt32() : null;
          Guid = Flags.HasFlag(FileFlags.Guid) ? new Guid(br.ReadBytes(16)) : null;
        }
      }
      else
      {
        stream.Seek(0, SeekOrigin.Begin);
      }
    }

    public virtual byte[] ToByteArray()
    {
      using (var stream = new MemoryStream())
      {
        if (Flags != FileFlags.None)
        {
          using (var writer = new BinaryWriter(stream, Encoding.GetEncoding("ISO-8859-2")))
          {
            writer.Write(Header);
            writer.Write((byte)Flags);
            if (Flags.HasFlag(FileFlags.Named))
            {
              writer.Write(TranslationId);
            }
            if (Flags.HasFlag(FileFlags.Resource))
            {
              writer.Write((byte)ResourceType);
            }
            if (Flags.HasFlag(FileFlags.Guid))
            {
              writer.Write(Guid.Value.ToByteArray());
            }
          }
        }
        return stream.ToArray();
      }
    }

    private bool IsEarthFormat(Stream stream)
    {
      var header = new byte[Header.Length];
      stream.Read(header, 0, Header.Length);
      return header.AsSpan().SequenceEqual(Header);
    }
  }
}
