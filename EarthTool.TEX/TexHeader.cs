using System.Diagnostics;
using System.IO;

namespace EarthTool.TEX
{
  public class TexHeader
  {
    public TexFlags Flags;
    public int Width { get; }
    public int Height { get; }
    public int SlideCount { get; } = 1;
    public int CursorX { get; }
    public int CursorY { get; }
    public int CursorAnimationType { get; }
    public int CursorFrameTime { get; }
    public int LodCount { get; }
    public int DestroyedCount { get; } = 1;
    public int Magic { get; }

    public TexHeader(BinaryReader reader)
    {
      Flags = (TexFlags)reader.ReadUInt32();
      if (Flags.HasFlag(TexFlags.DamageStates))
      {
        DestroyedCount = reader.ReadInt32();
      }

      if (Flags.HasFlag(TexFlags.Container) || Flags.HasFlag(TexFlags.SideColors))
      {
        SlideCount = reader.ReadInt32();
      }

      if (Flags.HasFlag(TexFlags.Mipmap) && !Flags.HasFlag(TexFlags.DamageStates))
      {
        Magic = reader.ReadInt32();
        Debug.Assert(Magic == 0x8888);
        Width = reader.ReadInt32();
        Height = reader.ReadInt32();
      }

      if (Flags.HasFlag(TexFlags.Cursor))
      {
        CursorX = reader.ReadInt32();
        CursorY = reader.ReadInt32();
        CursorAnimationType = reader.ReadInt32();
        CursorFrameTime = reader.ReadInt32();
      }

      if (Flags.HasFlag(TexFlags.Lod))
      {
        LodCount = reader.ReadInt32();
      }
    }

    public byte[] GetBytes()
    {
      using (var ms = new MemoryStream())
      {
        using (var writer = new BinaryWriter(ms))
        {
          writer.Write((uint)Flags);
          writer.Write(Flags.HasFlag(TexFlags.Container) ? SlideCount : 0x8888);
          if (!Flags.HasFlag(TexFlags.Container))
          {
            writer.Write(Width);
            writer.Write(Height);
          }
        }

        return ms.ToArray();
      }
    }
  }
}
