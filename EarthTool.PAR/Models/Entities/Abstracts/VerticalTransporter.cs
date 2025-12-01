using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

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
      VehicleSpeed = data.ReadInteger();
      VerticalVehicleAnimationType = (VerticalVehicleAnimationType)data.ReadInteger();
    }

    public int VehicleSpeed { get; set; }

    public VerticalVehicleAnimationType VerticalVehicleAnimationType { get; set; }

    [JsonIgnore]
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
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write(VehicleSpeed);
      bw.Write((int)VerticalVehicleAnimationType);

      return output.ToArray();
    }
  }
}
