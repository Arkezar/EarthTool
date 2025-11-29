using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class MultiExplosion : InteractableEntity
  {
    public MultiExplosion()
    {
    }

    public MultiExplosion(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      UseDownBuilding = ReadInteger(data);
      DownBuildingStart = ReadInteger(data);
      DownBuildingTime = ReadInteger(data);
      SubObject1 = ReadStringRef(data);
      Time1 = ReadInteger(data);
      Angle1 = ReadInteger(data);
      Dist4X1 = ReadInteger(data);
      SubObject2 = ReadStringRef(data);
      Time2 = ReadInteger(data);
      Angle2 = ReadInteger(data);
      Dist4X2 = ReadInteger(data);
      SubObject3 = ReadStringRef(data);
      Time3 = ReadInteger(data);
      Angle3 = ReadInteger(data);
      Dist4X3 = ReadInteger(data);
      SubObject4 = ReadStringRef(data);
      Time4 = ReadInteger(data);
      Angle4 = ReadInteger(data);
      Dist4X4 = ReadInteger(data);
      SubObject5 = ReadStringRef(data);
      Time5 = ReadInteger(data);
      Angle5 = ReadInteger(data);
      Dist4X5 = ReadInteger(data);
      SubObject6 = ReadStringRef(data);
      Time6 = ReadInteger(data);
      Angle6 = ReadInteger(data);
      Dist4X6 = ReadInteger(data);
      SubObject7 = ReadStringRef(data);
      Time7 = ReadInteger(data);
      Angle7 = ReadInteger(data);
      Dist4X7 = ReadInteger(data);
      SubObject8 = ReadStringRef(data);
      Time8 = ReadInteger(data);
      Angle8 = ReadInteger(data);
      Dist4X8 = ReadInteger(data);
    }

    public int UseDownBuilding { get; set; }

    public int DownBuildingStart { get; set; }

    public int DownBuildingTime { get; set; }

    public string SubObject1 { get; set; }

    public int Time1 { get; set; }

    public int Angle1 { get; set; }

    public int Dist4X1 { get; set; }

    public string SubObject2 { get; set; }

    public int Time2 { get; set; }

    public int Angle2 { get; set; }

    public int Dist4X2 { get; set; }

    public string SubObject3 { get; set; }

    public int Time3 { get; set; }

    public int Angle3 { get; set; }

    public int Dist4X3 { get; set; }

    public string SubObject4 { get; set; }

    public int Time4 { get; set; }

    public int Angle4 { get; set; }

    public int Dist4X4 { get; set; }

    public string SubObject5 { get; set; }

    public int Time5 { get; set; }

    public int Angle5 { get; set; }

    public int Dist4X5 { get; set; }

    public string SubObject6 { get; set; }

    public int Time6 { get; set; }

    public int Angle6 { get; set; }

    public int Dist4X6 { get; set; }

    public string SubObject7 { get; set; }

    public int Time7 { get; set; }

    public int Angle7 { get; set; }

    public int Dist4X7 { get; set; }

    public string SubObject8 { get; set; }
    public int Time8 { get; set; }
    public int Angle8 { get; set; }
    public int Dist4X8 { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => UseDownBuilding,
        () => DownBuildingStart,
        () => DownBuildingTime,
        () => SubObject1,
        () => ReferenceMarker,
        () => Time1,
        () => Angle1,
        () => Dist4X1,
        () => SubObject2,
        () => ReferenceMarker,
        () => Time2,
        () => Angle2,
        () => Dist4X2,
        () => SubObject3,
        () => ReferenceMarker,
        () => Time3,
        () => Angle3,
        () => Dist4X3,
        () => SubObject4,
        () => ReferenceMarker,
        () => Time4,
        () => Angle4,
        () => Dist4X4,
        () => SubObject5,
        () => ReferenceMarker,
        () => Time5,
        () => Angle5,
        () => Dist4X5,
        () => SubObject6,
        () => ReferenceMarker,
        () => Time6,
        () => Angle6,
        () => Dist4X6,
        () => SubObject7,
        () => ReferenceMarker,
        () => Time7,
        () => Angle7,
        () => Dist4X7,
        () => SubObject8,
        () => ReferenceMarker,
        () => Time8,
        () => Angle8,
        () => Dist4X8
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write(UseDownBuilding);
      bw.Write(DownBuildingStart);
      bw.Write(DownBuildingTime);
      WriteStringRef(bw, SubObject1, encoding);
      bw.Write(Time1);
      bw.Write(Angle1);
      bw.Write(Dist4X1);
      WriteStringRef(bw, SubObject2, encoding);
      bw.Write(Time2);
      bw.Write(Angle2);
      bw.Write(Dist4X2);
      WriteStringRef(bw, SubObject3, encoding);
      bw.Write(Time3);
      bw.Write(Angle3);
      bw.Write(Dist4X3);
      WriteStringRef(bw, SubObject4, encoding);
      bw.Write(Time4);
      bw.Write(Angle4);
      bw.Write(Dist4X4);
      WriteStringRef(bw, SubObject5, encoding);
      bw.Write(Time5);
      bw.Write(Angle5);
      bw.Write(Dist4X5);
      WriteStringRef(bw, SubObject6, encoding);
      bw.Write(Time6);
      bw.Write(Angle6);
      bw.Write(Dist4X6);
      WriteStringRef(bw, SubObject7, encoding);
      bw.Write(Time7);
      bw.Write(Angle7);
      bw.Write(Dist4X7);
      WriteStringRef(bw, SubObject8, encoding);
      bw.Write(Time8);
      bw.Write(Angle8);
      bw.Write(Dist4X8);

      return output.ToArray();
    }
  }
}
