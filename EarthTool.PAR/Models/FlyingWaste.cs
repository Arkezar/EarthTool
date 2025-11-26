using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class FlyingWaste : DestructibleEntity
  {
    public FlyingWaste()
    {
    }

    public FlyingWaste(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
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

    public int WasteSize { get; set; }

    public string SubWasteId1 { get; set; }

    public int SubWaste1Alpha { get; set; }

    public string SubWasteId2 { get; set; }

    public int SubWaste2Alpha { get; set; }

    public string SubWasteId3 { get; set; }

    public int SubWaste3Alpha { get; set; }

    public string SubWasteId4 { get; set; }

    public int SubWaste4Alpha { get; set; }

    public int FlightTime { get; set; }

    public int WasteSpeed { get; set; }

    public int WasteDistanceX4 { get; set; }

    public int WasteBeta { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => WasteSize,
        () => SubWasteId1,
        () => 1,
        () => SubWaste1Alpha,
        () => SubWasteId2,
        () => 1,
        () => SubWaste2Alpha,
        () => SubWasteId3,
        () => 1,
        () => SubWaste3Alpha,
        () => SubWasteId4,
        () => 1,
        () => SubWaste4Alpha,
        () => FlightTime,
        () => WasteSpeed,
        () => WasteDistanceX4,
        () => WasteBeta
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
