using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class BuildingTransporter : VerticalTransporter
  {
    public BuildingTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      BuilderLineId = GetString(data);
      data.ReadBytes(4);
    }

    public string BuilderLineId { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(BuilderLineId.Length);
          bw.Write(encoding.GetBytes(BuilderLineId));
          bw.Write(-1);
        }
        return output.ToArray();
      }
    }
  }
}
