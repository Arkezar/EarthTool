using EarthTool.PAR.Enums;
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
      Mesh1 = ReadString(data);
      Mesh2 = ReadString(data);
      Mesh3 = ReadString(data);
      SmokeTime1 = ReadInteger(data);
      SmokeTime2 = ReadInteger(data);
      SmokeTime3 = ReadInteger(data);
      SmokeFrequency = ReadInteger(data);
      StartingTime = ReadInteger(data);
      SmokingTime = ReadInteger(data);
      EndingTime = ReadInteger(data);
      SmokeUpSpeed = ReadInteger(data);
      NewSmokeDistance = ReadInteger(data);
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
      WriteString(bw, Mesh1, encoding);
      WriteString(bw, Mesh2, encoding);
      WriteString(bw, Mesh3, encoding);
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
