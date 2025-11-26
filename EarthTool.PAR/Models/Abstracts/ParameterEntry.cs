using System.IO;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class ParameterEntry
  {
    public string Name { get; set; }
    
    protected static  int GetInteger(BinaryReader data)
    {
      return data.ReadInt32();
    }
    
    protected static uint GetUnsignedInteger(BinaryReader data)
    {
      return data.ReadUInt32();
    }

    protected static string GetString(BinaryReader data)
    {
      return new string(data.ReadChars(data.ReadInt32()));
    }
  }
}
