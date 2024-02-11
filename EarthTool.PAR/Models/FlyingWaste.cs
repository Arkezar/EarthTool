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
    public FlyingWaste(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      WasteSize = GetInteger(data);
      SubWasteId1 = GetString(data);
      data.ReadBytes(4);
      SubWaste1Alpha = GetInteger(data);
      SubWasteId2 = GetString(data);
      data.ReadBytes(4);
      SubWaste2Alpha = GetInteger(data);
      SubWasteId3 = GetString(data);
      data.ReadBytes(4);
      SubWaste3Alpha = GetInteger(data);
      SubWasteId4 = GetString(data);
      data.ReadBytes(4);
      SubWaste4Alpha = GetInteger(data);
      FlightTime = GetInteger(data);
      WasteSpeed = GetInteger(data);
      WasteDistanceX4 = GetInteger(data);
      WasteBeta = GetInteger(data);
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

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(WasteSize);
          bw.Write(SubWasteId1.Length);
          bw.Write(encoding.GetBytes(SubWasteId1));
          bw.Write(-1);
          bw.Write(SubWaste1Alpha);
          bw.Write(SubWasteId2.Length);
          bw.Write(encoding.GetBytes(SubWasteId2));
          bw.Write(-1);
          bw.Write(SubWaste2Alpha);
          bw.Write(SubWasteId3.Length);
          bw.Write(encoding.GetBytes(SubWasteId3));
          bw.Write(-1);
          bw.Write(SubWaste3Alpha);
          bw.Write(SubWasteId4.Length);
          bw.Write(encoding.GetBytes(SubWasteId4));
          bw.Write(-1);
          bw.Write(SubWaste4Alpha);
          bw.Write(FlightTime);
          bw.Write(WasteSpeed);
          bw.Write(WasteDistanceX4);
          bw.Write(WasteBeta);
        }
        return output.ToArray();
      }
    }
  }
}
