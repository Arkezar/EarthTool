using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Parameter : Entity
  {
    public Parameter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, fieldTypes)
    {
      Values = fieldTypes.Select(s => s ? (object)GetString(data) : GetInteger(data)).ToList();
    }

    public IEnumerable<object> Values { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          foreach (var value in Values)
          {
            if (value is string stringValue)
            {
              bw.Write(stringValue.Length);
              bw.Write(encoding.GetBytes(stringValue));
            }
            else if (value is int intValue)
            {
              bw.Write(intValue);
            }
          }
        }
        return output.ToArray();
      }
    }
  }
}
