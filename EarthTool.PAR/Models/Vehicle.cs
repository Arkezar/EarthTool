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
    public Vehicle(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
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
  }
}
