using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class ResourceTransporter : VerticalTransporter
  {
    public ResourceTransporter()
    {
    }

    public ResourceTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      ResourceVehicleType = (ResourceVehicleType)ReadInteger(data);
      AnimatedTransporterStop = ReadInteger(data);
      ShowVideoPerTransportersCount = ReadInteger(data);
      TotalOrbitalMoney = ReadInteger(data);
    }

    public ResourceVehicleType ResourceVehicleType { get; set; }

    public int AnimatedTransporterStop { get; set; }

    public int ShowVideoPerTransportersCount { get; set; }

    public int TotalOrbitalMoney { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => ResourceVehicleType,
        () => AnimatedTransporterStop,
        () => ShowVideoPerTransportersCount,
        () => TotalOrbitalMoney
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write((int)ResourceVehicleType);
      bw.Write(AnimatedTransporterStop);
      bw.Write(ShowVideoPerTransportersCount);
      bw.Write(TotalOrbitalMoney);

      return output.ToArray();
    }
  }
}
