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
      SoilSpeed = GetInteger(data);
      RoadSpeed = GetInteger(data);
      SandSpeed = GetInteger(data);
      BankSpeed = GetInteger(data);
      WaterSpeed = GetInteger(data);
      DeepWaterSpeed = GetInteger(data);
      AirSpeed = GetInteger(data);
      ObjectType = GetInteger(data);
      EngineSmokeId = GetString(data);
      data.ReadBytes(4);
      DustId = GetString(data);
      data.ReadBytes(4);
      BillowId = GetString(data);
      data.ReadBytes(4);
      StandBillowId = GetString(data);
      data.ReadBytes(4);
      TrackId = GetString(data);
      data.ReadBytes(4);
    }

    public int SoilSpeed { get; set; }

    public int RoadSpeed { get; set; }

    public int SandSpeed { get; set; }

    public int BankSpeed { get; set; }

    public int WaterSpeed { get; set; }

    public int DeepWaterSpeed { get; set; }

    public int AirSpeed { get; set; }

    public int ObjectType { get; set; }

    public string EngineSmokeId { get; set; }

    public string DustId { get; set; }

    public string BillowId { get; set; }

    public string StandBillowId { get; set; }

    public string TrackId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => SoilSpeed,
        () => RoadSpeed,
        () => SandSpeed,
        () => BankSpeed,
        () => WaterSpeed,
        () => DeepWaterSpeed,
        () => AirSpeed,
        () => ObjectType,
        () => EngineSmokeId,
        () => 1,
        () => DustId,
        () => 1,
        () => BillowId,
        () => 1,
        () => StandBillowId,
        () => 1,
        () => TrackId,
        () => 1
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
          bw.Write(SoilSpeed);
          bw.Write(RoadSpeed);
          bw.Write(SandSpeed);
          bw.Write(BankSpeed);
          bw.Write(WaterSpeed);
          bw.Write(DeepWaterSpeed);
          bw.Write(AirSpeed);
          bw.Write(ObjectType);
          bw.Write(EngineSmokeId.Length);
          bw.Write(encoding.GetBytes(EngineSmokeId));
          bw.Write(-1);
          bw.Write(DustId.Length);
          bw.Write(encoding.GetBytes(DustId));
          bw.Write(-1);
          bw.Write(BillowId.Length);
          bw.Write(encoding.GetBytes(BillowId));
          bw.Write(-1);
          bw.Write(StandBillowId.Length);
          bw.Write(encoding.GetBytes(StandBillowId));
          bw.Write(-1);
          bw.Write(TrackId.Length);
          bw.Write(encoding.GetBytes(TrackId));
          bw.Write(-1);
        }

        return output.ToArray();
      }
    }
  }
}
