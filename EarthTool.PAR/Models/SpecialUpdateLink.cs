using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class SpecialUpdateLink : Entity
  {
    public SpecialUpdateLink(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, fieldTypes)
    {
      Value = GetString(data);
      data.ReadBytes(4);
    }

    public string Value { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(Value.Length);
          bw.Write(encoding.GetBytes(Value));
          bw.Write(-1);
        }
        return output.ToArray();
      }
    }
  }
}
