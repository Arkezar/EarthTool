using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using System;
using System.IO;
using System.Text;

namespace EarthTool.Common.Models
{
  internal class EarthInfo : IEarthInfo
  {
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
      ValidateFlags();
      using (MemoryStream stream = new())
      {
        if (Flags != FileFlags.None)
        {
          using (BinaryWriter writer = new(stream, encoding))
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

    public object Clone()
        => MemberwiseClone();

    private void ValidateFlags()
    {
      if (!string.IsNullOrEmpty(TranslationId)) SetFlag(FileFlags.Named);
      if (ResourceType.HasValue) SetFlag(FileFlags.Resource);
      if (Guid.HasValue) SetFlag(FileFlags.Guid);
    }

    public static IEarthInfo WdArchiveHeader => new EarthInfo()
    {
      Flags = FileFlags.Compressed | FileFlags.Resource | FileFlags.Guid,
      ResourceType = Enums.ResourceType.WdArchive
    };
  }
}