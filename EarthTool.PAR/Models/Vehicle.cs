using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Vehicle : EquipableEntity
  {
    public Vehicle()
    {
    }

    public Vehicle(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      SoilSpeed = data.ReadInteger();
      RoadSpeed = data.ReadInteger();
      SandSpeed = data.ReadInteger();
      BankSpeed = data.ReadInteger();
      WaterSpeed = data.ReadInteger();
      DeepWaterSpeed = data.ReadInteger();
      AirSpeed = data.ReadInteger();
      ObjectType = (VehicleObjectType)data.ReadInteger();
      EngineSmokeId = data.ReadParameterStringRef();
      DustId = data.ReadParameterStringRef();
      BillowId = data.ReadParameterStringRef();
      StandBillowId = data.ReadParameterStringRef();
      TrackId = data.ReadParameterStringRef();
    }

    public int SoilSpeed { get; set; }

    public int RoadSpeed { get; set; }

    public int SandSpeed { get; set; }

    public int BankSpeed { get; set; }

    public int WaterSpeed { get; set; }

    public int DeepWaterSpeed { get; set; }

    public int AirSpeed { get; set; }

    public VehicleObjectType ObjectType { get; set; }

    public string EngineSmokeId { get; set; }

    public string DustId { get; set; }

    public string BillowId { get; set; }

    public string StandBillowId { get; set; }

    public string TrackId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get
        => base.FieldTypes.Concat(IsStringMember(
          () => SoilSpeed,
          () => RoadSpeed,
          () => SandSpeed,
          () => BankSpeed,
          () => WaterSpeed,
          () => DeepWaterSpeed,
          () => AirSpeed,
          () => ObjectType,
          () => EngineSmokeId,
          () => ReferenceMarker,
          () => DustId,
          () => ReferenceMarker,
          () => BillowId,
          () => ReferenceMarker,
          () => StandBillowId,
          () => ReferenceMarker,
          () => TrackId,
          () => ReferenceMarker
        ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write(SoilSpeed);
      bw.Write(RoadSpeed);
      bw.Write(SandSpeed);
      bw.Write(BankSpeed);
      bw.Write(WaterSpeed);
      bw.Write(DeepWaterSpeed);
      bw.Write(AirSpeed);
      bw.Write((int)ObjectType);
      bw.WriteParameterStringRef(EngineSmokeId, encoding);
      bw.WriteParameterStringRef(DustId, encoding);
      bw.WriteParameterStringRef(BillowId, encoding);
      bw.WriteParameterStringRef(StandBillowId, encoding);
      bw.WriteParameterStringRef(TrackId, encoding);

      return output.ToArray();
    }
  }
}
