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
  public class Smoke : DestructibleEntity
  {
    public Smoke()
    {
    }

    public Smoke(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      Mesh1 = data.ReadParameterString();
      Mesh2 = data.ReadParameterString();
      Mesh3 = data.ReadParameterString();
      SmokeTime1 = data.ReadInteger();
      SmokeTime2 = data.ReadInteger();
      SmokeTime3 = data.ReadInteger();
      SmokeFrequency = data.ReadInteger();
      StartingTime = data.ReadInteger();
      SmokingTime = data.ReadInteger();
      EndingTime = data.ReadInteger();
      SmokeUpSpeed = data.ReadInteger();
      NewSmokeDistance = data.ReadInteger();
    }

    public string Mesh1 { get; set; }

    public string Mesh2 { get; set; }

    public string Mesh3 { get; set; }

    public int SmokeTime1 { get; set; }

    public int SmokeTime2 { get; set; }

    public int SmokeTime3 { get; set; }

    public int SmokeFrequency { get; set; }

    public int StartingTime { get; set; }

    public int SmokingTime { get; set; }

    public int EndingTime { get; set; }

    public int SmokeUpSpeed { get; set; }

    public int NewSmokeDistance { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => Mesh1,
        () => Mesh2,
        () => Mesh3,
        () => SmokeTime1,
        () => SmokeTime2,
        () => SmokeTime3,
        () => SmokeFrequency,
        () => StartingTime,
        () => SmokingTime,
        () => EndingTime,
        () => SmokeUpSpeed,
        () => NewSmokeDistance
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.WriteParameterString(Mesh1, encoding);
      bw.WriteParameterString(Mesh2, encoding);
      bw.WriteParameterString(Mesh3, encoding);
      bw.Write(SmokeTime1);
      bw.Write(SmokeTime2);
      bw.Write(SmokeTime3);
      bw.Write(SmokeFrequency);
      bw.Write(StartingTime);
      bw.Write(SmokingTime);
      bw.Write(EndingTime);
      bw.Write(SmokeUpSpeed);
      bw.Write(NewSmokeDistance);

      return output.ToArray();
    }
  }
}
