using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public abstract class VerticalTransporter : EquipableEntity
  {
    public VerticalTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      VehicleSpeed = GetInteger(data);
      VerticalVehicleAnimationType = GetInteger(data);
    }

    public int VehicleSpeed { get; }

    public int VerticalVehicleAnimationType { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(VehicleSpeed);
          bw.Write(VerticalVehicleAnimationType);
        }
        return output.ToArray();
      }
    }
  }
}
