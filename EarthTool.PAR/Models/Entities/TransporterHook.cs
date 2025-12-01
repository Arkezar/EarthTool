using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class TransporterHook : Equipment
  {
    public TransporterHook()
    {
    }

    public TransporterHook(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      AnimTransporterDownStart = data.ReadInteger();
      AnimTransporterDownEnd = data.ReadInteger();
      AnimTransporterUpStart = data.ReadInteger();
      AnimTransporterUpEnd = data.ReadInteger();
      AngleToGetPut = data.ReadInteger();
      AngleOfGetUnitByLandTransporter = data.ReadInteger();
      TakeHeight = data.ReadInteger();
    }

    public int AnimTransporterDownStart { get; set; }

    public int AnimTransporterDownEnd { get; set; }

    public int AnimTransporterUpStart { get; set; }

    public int AnimTransporterUpEnd { get; set; }

    public int AngleToGetPut { get; set; }

    public int AngleOfGetUnitByLandTransporter { get; set; }

    public int TakeHeight { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => AnimTransporterDownStart,
        () => AnimTransporterDownEnd,
        () => AnimTransporterUpStart,
        () => AnimTransporterUpEnd,
        () => AngleToGetPut,
        () => AngleOfGetUnitByLandTransporter,
        () => TakeHeight
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write(AnimTransporterDownStart);
      bw.Write(AnimTransporterDownEnd);
      bw.Write(AnimTransporterUpStart);
      bw.Write(AnimTransporterUpEnd);
      bw.Write(AngleToGetPut);
      bw.Write(AngleOfGetUnitByLandTransporter);
      bw.Write(TakeHeight);

      return output.ToArray();
    }
  }
}
