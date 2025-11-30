using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
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
      UseDownBuilding = data.ReadInteger();
      DownBuildingStart = data.ReadInteger();
      DownBuildingTime = data.ReadInteger();
      SubObject1 = data.ReadParameterStringRef();
      Time1 = data.ReadInteger();
      Angle1 = data.ReadInteger();
      Dist4X1 = data.ReadInteger();
      SubObject2 = data.ReadParameterStringRef();
      Time2 = data.ReadInteger();
      Angle2 = data.ReadInteger();
      Dist4X2 = data.ReadInteger();
      SubObject3 = data.ReadParameterStringRef();
      Time3 = data.ReadInteger();
      Angle3 = data.ReadInteger();
      Dist4X3 = data.ReadInteger();
      SubObject4 = data.ReadParameterStringRef();
      Time4 = data.ReadInteger();
      Angle4 = data.ReadInteger();
      Dist4X4 = data.ReadInteger();
      SubObject5 = data.ReadParameterStringRef();
      Time5 = data.ReadInteger();
      Angle5 = data.ReadInteger();
      Dist4X5 = data.ReadInteger();
      SubObject6 = data.ReadParameterStringRef();
      Time6 = data.ReadInteger();
      Angle6 = data.ReadInteger();
      Dist4X6 = data.ReadInteger();
      SubObject7 = data.ReadParameterStringRef();
      Time7 = data.ReadInteger();
      Angle7 = data.ReadInteger();
      Dist4X7 = data.ReadInteger();
      SubObject8 = data.ReadParameterStringRef();
      Time8 = data.ReadInteger();
      Angle8 = data.ReadInteger();
      Dist4X8 = data.ReadInteger();
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
      bw.WriteParameterStringRef(SubObject1, encoding);
      bw.Write(Time1);
      bw.Write(Angle1);
      bw.Write(Dist4X1);
      bw.WriteParameterStringRef(SubObject2, encoding);
      bw.Write(Time2);
      bw.Write(Angle2);
      bw.Write(Dist4X2);
      bw.WriteParameterStringRef(SubObject3, encoding);
      bw.Write(Time3);
      bw.Write(Angle3);
      bw.Write(Dist4X3);
      bw.WriteParameterStringRef(SubObject4, encoding);
      bw.Write(Time4);
      bw.Write(Angle4);
      bw.Write(Dist4X4);
      bw.WriteParameterStringRef(SubObject5, encoding);
      bw.Write(Time5);
      bw.Write(Angle5);
      bw.Write(Dist4X5);
      bw.WriteParameterStringRef(SubObject6, encoding);
      bw.Write(Time6);
      bw.Write(Angle6);
      bw.Write(Dist4X6);
      bw.WriteParameterStringRef(SubObject7, encoding);
      bw.Write(Time7);
      bw.Write(Angle7);
      bw.Write(Dist4X7);
      bw.WriteParameterStringRef(SubObject8, encoding);
      bw.Write(Time8);
      bw.Write(Angle8);
      bw.Write(Dist4X8);

      return output.ToArray();
    }
  }
}
