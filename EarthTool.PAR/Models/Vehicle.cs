using EarthTool.PAR.Enums;
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
      SoilSpeed = ReadInteger(data);
      RoadSpeed = ReadInteger(data);
      SandSpeed = ReadInteger(data);
      BankSpeed = ReadInteger(data);
      WaterSpeed = ReadInteger(data);
      DeepWaterSpeed = ReadInteger(data);
      AirSpeed = ReadInteger(data);
      ObjectType = (VehicleObjectType)ReadInteger(data);
      EngineSmokeId = ReadStringRef(data);
      DustId = ReadStringRef(data);
      BillowId = ReadStringRef(data);
      StandBillowId = ReadStringRef(data);
      TrackId = ReadStringRef(data);
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
      WriteStringRef(bw, EngineSmokeId, encoding);
      WriteStringRef(bw, DustId, encoding);
      WriteStringRef(bw, BillowId, encoding);
      WriteStringRef(bw, StandBillowId, encoding);
      WriteStringRef(bw, TrackId, encoding);

      return output.ToArray();
    }
  }
}
