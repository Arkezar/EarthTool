using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class FlyingWaste : DestructibleEntity
  {
    public FlyingWaste(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      WasteSize = BitConverter.ToInt32(data.ReadBytes(4));
      SubWasteId1 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      SubWaste1Alpha = BitConverter.ToInt32(data.ReadBytes(4));
      SubWasteId2 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      SubWaste2Alpha = BitConverter.ToInt32(data.ReadBytes(4));
      SubWasteId3 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      SubWaste3Alpha = BitConverter.ToInt32(data.ReadBytes(4));
      SubWasteId4 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      SubWaste4Alpha = BitConverter.ToInt32(data.ReadBytes(4));
      FlightTime = BitConverter.ToInt32(data.ReadBytes(4));
      WasteSpeed = BitConverter.ToInt32(data.ReadBytes(4));
      WasteDistanceX4 = BitConverter.ToInt32(data.ReadBytes(4));
      WasteBeta = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int WasteSize { get; }

    public string SubWasteId1 { get; }

    public int SubWaste1Alpha { get; }

    public string SubWasteId2 { get; }

    public int SubWaste2Alpha { get; }

    public string SubWasteId3 { get; }

    public int SubWaste3Alpha { get; }

    public string SubWasteId4 { get; }

    public int SubWaste4Alpha { get; }

    public int FlightTime { get; }

    public int WasteSpeed { get; }

    public int WasteDistanceX4 { get; }

    public int WasteBeta { get; }
  }
}
