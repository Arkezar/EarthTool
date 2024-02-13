using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using System;
using System.IO;
using System.Text;

namespace EarthTool.Common.Models
{
  public class EarthInfo : IEarthInfo
  {
    public string FilePath { get; set; }

    public FileFlags Flags { get; set; }

    public string TranslationId { get; set; }

    public void SetFlag(FileFlags flag)
      => Flags |= flag;

    public void RemoveFlag(FileFlags flag)
      => Flags &= ~flag;

    public ResourceType? ResourceType { get; set; }

    public Guid? Guid { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        if (Flags != FileFlags.None)
        {
          using (var writer = new BinaryWriter(stream, encoding))
          {
            writer.Write(Identifiers.Info);
            writer.Write((byte)Flags);
            if (Flags.HasFlag(FileFlags.Named))
            {
              writer.Write(TranslationId);
            }

            if (Flags.HasFlag(FileFlags.Resource))
            {
              writer.Write((int)ResourceType);
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
  }
}