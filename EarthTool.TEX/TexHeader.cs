using System.IO;

namespace EarthTool.TEX
{
  public class TexHeader
  {
    public TextureType TextureType { get; }
    public AnimationType AnimationType { get; }
    public TextureSubType SubType { get; }
    public byte Reserved1 { get; }
    public byte Unknown1 { get; }
    public byte Unknown2 { get; }
    public byte Reserved2 { get; }
    public byte Reserved3 { get; }
    public int ElementCount { get; }
    public int GroupCount { get; }
    public int Width { get; }
    public int Height { get; }
    public int LodLevels { get; }

    public TexHeader(BinaryReader reader)
    {
      TextureType = (TextureType)reader.ReadByte();
      AnimationType = (AnimationType)reader.ReadByte();
      Reserved1 = reader.ReadByte();
      SubType = (TextureSubType)reader.ReadByte();
      if (SubType.HasFlag(TextureSubType.Unknown1 | TextureSubType.Unknown2)
          && !SubType.HasFlag(TextureSubType.Collection))
      {
        Unknown1 = reader.ReadByte();
        Unknown2 = reader.ReadByte();
        Reserved2 = reader.ReadByte();
        Reserved3 = reader.ReadByte();
      }

      if (SubType.HasFlag(TextureSubType.Collection) || SubType.HasFlag(TextureSubType.Sides))
      {
        ElementCount = reader.ReadInt32();
      }

      if (SubType.HasFlag(TextureSubType.Grouped))
      {
        GroupCount = reader.ReadInt32();
      }

      if (TextureType.HasFlag(TextureType.Texture) && !SubType.HasFlag(TextureSubType.Collection))
      {
        Width = reader.ReadInt32();
        Height = reader.ReadInt32();
        if (TextureType.HasFlag(TextureType.Lod))
        {
          LodLevels = reader.ReadInt32();
        }
      }
    }

    public byte[] GetBytes()
    {
      using (var ms = new MemoryStream())
      {
        using (var writer = new BinaryWriter(ms))
        {
          writer.Write((byte)TextureType);
          writer.Write((byte)AnimationType);
          writer.Write(Reserved1);
          writer.Write((byte)SubType);
          if (SubType.HasFlag(TextureSubType.Unknown1 | TextureSubType.Unknown2)
              && !SubType.HasFlag(TextureSubType.Collection))
          {
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            writer.Write(Reserved2);
            writer.Write(Reserved3);
          }

          if (SubType.HasFlag(TextureSubType.Collection) || SubType.HasFlag(TextureSubType.Sides))
          {
            writer.Write(ElementCount);
          }

          if (SubType.HasFlag(TextureSubType.Grouped))
          {
            writer.Write(GroupCount);
          }
          
          if (TextureType.HasFlag(TextureType.Texture) && !SubType.HasFlag(TextureSubType.Collection))
          {
            writer.Write(Width);
            writer.Write(Height);
            if (TextureType.HasFlag(TextureType.Lod))
            {
              writer.Write(LodLevels);
            }
          }
        }

        return ms.ToArray();
      }
    }
  }
}