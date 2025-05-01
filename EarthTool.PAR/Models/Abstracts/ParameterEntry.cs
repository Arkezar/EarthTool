using System.IO;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class ParameterEntry
  {
    protected int GetInteger(BinaryReader data)
    {
      return data.ReadInt32();
    }
    
    protected uint GetUnsignedInteger(BinaryReader data)
    {
      return data.ReadUInt32();
    }

    protected string GetString(BinaryReader data)
    {
      return new string(data.ReadChars(data.ReadInt32()));
    }
  }
}