using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class SupplyTransporter : Vehicle
  {
    public SupplyTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      AmmoCapacity = GetInteger(data);
      AnimSupplyDownStart = GetInteger(data);
      AnimSupplyDownEnd = GetInteger(data);
      AnimSupplyUpStart = GetInteger(data);
      AnimSupplyUpEnd = GetInteger(data);
    }

    public int AmmoCapacity { get; }

    public int AnimSupplyDownStart { get; }

    public int AnimSupplyDownEnd { get; }

    public int AnimSupplyUpStart { get; }

    public int AnimSupplyUpEnd { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(AmmoCapacity);
          bw.Write(AnimSupplyDownStart);
          bw.Write(AnimSupplyDownEnd);
          bw.Write(AnimSupplyUpStart);
          bw.Write(AnimSupplyUpEnd);
        }
        return output.ToArray();
      }
    }
  }
}
