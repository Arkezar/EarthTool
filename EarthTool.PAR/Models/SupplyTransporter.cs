using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class SupplyTransporter : Vehicle
  {
    public SupplyTransporter()
    {
    }

    public SupplyTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      AmmoCapacity = GetInteger(data);
      AnimSupplyDownStart = GetInteger(data);
      AnimSupplyDownEnd = GetInteger(data);
      AnimSupplyUpStart = GetInteger(data);
      AnimSupplyUpEnd = GetInteger(data);
    }

    public int AmmoCapacity { get; set; }

    public int AnimSupplyDownStart { get; set; }

    public int AnimSupplyDownEnd { get; set; }

    public int AnimSupplyUpStart { get; set; }

    public int AnimSupplyUpEnd { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => AmmoCapacity,
        () => AnimSupplyDownStart,
        () => AnimSupplyDownEnd,
        () => AnimSupplyUpStart,
        () => AnimSupplyUpEnd
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
          bw.Write(AmmoCapacity);
          bw.Write(AnimSupplyDownStart);
          bw.Write(AnimSupplyDownEnd);
          bw.Write(AnimSupplyUpStart);
          bw.Write(AnimSupplyUpEnd);
        }

        return output.ToArray();
      }
    }
  }
}
