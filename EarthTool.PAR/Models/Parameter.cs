using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Parameter : TypelessEntity
  {
    public Parameter()
    {
    }

    public Parameter(string name, IEnumerable<int> requiredResearch, BinaryReader data, IEnumerable<bool> fieldTypes)
      : base(name, requiredResearch)
    {
      FieldTypes = fieldTypes;
      Values = fieldTypes.Select(s => s ? GetString(data) : GetInteger(data).ToString()).ToList();
    }

    [JsonInclude] public override IEnumerable<bool> FieldTypes { get; set; }

    public IEnumerable<string> Values { get; set; }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          for (int i = 0; i < Values.Count(); i++)
          {
            bool isString = FieldTypes.ElementAt(i);
            if (isString)
            {
              bw.Write(Values.ElementAt(i).Length);
              bw.Write(encoding.GetBytes(Values.ElementAt(i)));
            }
            else
            {
              bw.Write(int.Parse(Values.ElementAt(i)));
            }
          }
        }

        return output.ToArray();
      }
    }
  }
}
