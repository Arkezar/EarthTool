using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class ResourceTransporter : VerticalTransporter
  {
    public ResourceTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      ResourceVehicleType = GetInteger(data);
      AnimatedTransporterStop = GetInteger(data);
      ShowVideoPerTransportersCount = GetInteger(data);
      TotalOrbitalMoney = GetInteger(data);
    }

    public int ResourceVehicleType { get; }

    public int AnimatedTransporterStop { get; }

    public int ShowVideoPerTransportersCount { get; }

    public int TotalOrbitalMoney { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(ResourceVehicleType);
          bw.Write(AnimatedTransporterStop);
          bw.Write(ShowVideoPerTransportersCount);
          bw.Write(TotalOrbitalMoney);
        }
        return output.ToArray();
      }
    }
  }
}
