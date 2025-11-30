using EarthTool.PAR.Extensions;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class ShieldGenerator : TypelessEntity
  {
    public ShieldGenerator()
    {
    }

    public ShieldGenerator(string name, IEnumerable<int> requiredResearch, BinaryReader data)
      : base(name, requiredResearch)
    {
      ShieldCost = data.ReadInteger();
      ShieldValue = data.ReadInteger();
      ReloadTime = data.ReadInteger();
      ShieldMeshName = data.ReadParameterString();
      ShieldMeshViewIndex = data.ReadInteger();
    }

    public int ShieldCost { get; set; }

    public int ShieldValue { get; set; }

    public int ReloadTime { get; set; }

    public string ShieldMeshName { get; set; }

    public int ShieldMeshViewIndex { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => IsStringMember(
        () => ShieldCost,
        () => ShieldValue,
        () => ReloadTime,
        () => ShieldMeshName,
        () => ShieldMeshViewIndex
      );
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write(ShieldCost);
      bw.Write(ShieldValue);
      bw.Write(ReloadTime);
      bw.WriteParameterString(ShieldMeshName, encoding);
      bw.Write(ShieldMeshViewIndex);

      return output.ToArray();
    }
  }
}
