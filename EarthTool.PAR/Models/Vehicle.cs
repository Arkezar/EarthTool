using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Vehicle : EquipableEntity
  {
    public Vehicle(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data,
      IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
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

    public int SoilSpeed { get; }

    public int RoadSpeed { get; }

    public int SandSpeed { get; }

    public int BankSpeed { get; }

    public int WaterSpeed { get; }

    public int DeepWaterSpeed { get; }

    public int AirSpeed { get; }

    public int ObjectType { get; }

    public string EngineSmokeId { get; }

    public string DustId { get; }

    public string BillowId { get; }

    public string StandBillowId { get; }

    public string TrackId { get; }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
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