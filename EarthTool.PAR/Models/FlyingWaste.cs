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
      WasteSize = (WasteSize)ReadInteger(data);
      SubWasteId1 = ReadStringRef(data);
      SubWaste1Alpha = ReadInteger(data);
      SubWasteId2 = ReadStringRef(data);
      SubWaste2Alpha = ReadInteger(data);
      SubWasteId3 = ReadStringRef(data);
      SubWaste3Alpha = ReadInteger(data);
      SubWasteId4 = ReadStringRef(data);
      SubWaste4Alpha = ReadInteger(data);
      FlightTime = ReadInteger(data);
      WasteSpeed = ReadInteger(data);
      WasteDistanceX4 = ReadInteger(data);
      WasteBeta = ReadInteger(data);
    }

    public WasteSize WasteSize { get; set; }

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
      get
        => base.FieldTypes.Concat(IsStringMember(
          () => WasteSize,
          () => SubWasteId1,
          () => ReferenceMarker,
          () => SubWaste1Alpha,
          () => SubWasteId2,
          () => ReferenceMarker,
          () => SubWaste2Alpha,
          () => SubWasteId3,
          () => ReferenceMarker,
          () => SubWaste3Alpha,
          () => SubWasteId4,
          () => ReferenceMarker,
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
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write((int)WasteSize);
      WriteStringRef(bw, SubWasteId1, encoding);
      bw.Write(SubWaste1Alpha);
      WriteStringRef(bw, SubWasteId2, encoding);
      bw.Write(SubWaste2Alpha);
      WriteStringRef(bw, SubWasteId3, encoding);
      bw.Write(SubWaste3Alpha);
      WriteStringRef(bw, SubWasteId4, encoding);
      bw.Write(SubWaste4Alpha);
      bw.Write(FlightTime);
      bw.Write(WasteSpeed);
      bw.Write(WasteDistanceX4);
      bw.Write(WasteBeta);

      return output.ToArray();
    }
  }
}
