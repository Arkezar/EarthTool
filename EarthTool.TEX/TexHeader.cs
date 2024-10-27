using System.IO;

namespace EarthTool.TEX
{
  public class TexHeader
  {
    public byte Type { get; }
    public byte Unknown1 { get; }
    public byte Unknown2 { get; }
    public byte Subtype { get; }
    public byte Unknown3 { get; }
    public byte Unknown4 { get; }
    public byte Unknown5 { get; }
    public byte Unknown6 { get; }
    public int NumberOfMaps { get; }

    public TexHeader(BinaryReader reader)
    {
      Type = reader.ReadByte();
      Unknown1 = reader.ReadByte();
      Subtype = reader.ReadByte();
      Unknown2 = reader.ReadByte();
      if (Unknown2 == 3)
      {
        Unknown3 = reader.ReadByte();
        Unknown4 = reader.ReadByte();
        Unknown5 = reader.ReadByte();
        Unknown6 = reader.ReadByte();
      }
      
      if (Unknown2 == 128 || Unknown2 == 67 || Unknown2 == 16)
      {
        NumberOfMaps = reader.ReadInt32();
      }

      if (Unknown2 == 192)
      {
        NumberOfMaps = reader.ReadInt32();
        NumberOfMaps *= reader.ReadInt32();
      }
    }
  }
}