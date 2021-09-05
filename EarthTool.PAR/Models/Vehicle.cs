using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Vehicle : EquipableEntity
  {
    public Vehicle(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      SoilSpeed = BitConverter.ToInt32(data.ReadBytes(4));
      RoadSpeed = BitConverter.ToInt32(data.ReadBytes(4));
      SandSpeed = BitConverter.ToInt32(data.ReadBytes(4));
      BankSpeed = BitConverter.ToInt32(data.ReadBytes(4));
      WaterSpeed = BitConverter.ToInt32(data.ReadBytes(4));
      DeepWaterSpeed = BitConverter.ToInt32(data.ReadBytes(4));
      AirSpeed = BitConverter.ToInt32(data.ReadBytes(4));
      ObjectType = BitConverter.ToInt32(data.ReadBytes(4));
      EngineSmokeId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      DustId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      BillowId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      StandBillowId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      TrackId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
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
  }
}
