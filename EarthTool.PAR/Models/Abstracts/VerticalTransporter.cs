using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class VerticalTransporter : EquipableEntity
  {
    public VerticalTransporter()
    {
    }

    public VerticalTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      VehicleSpeed = GetInteger(data);
      VerticalVehicleAnimationType = GetInteger(data);
    }

    public int VehicleSpeed { get; set; }

    public int VerticalVehicleAnimationType { get; set; }

    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => VehicleSpeed,
        () => VerticalVehicleAnimationType
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
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