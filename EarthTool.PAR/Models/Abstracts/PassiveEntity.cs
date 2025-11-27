using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class PassiveEntity : DestructibleEntity
  {
    public PassiveEntity()
    {
    }

    public PassiveEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      PassiveMask = (PassiveMask)GetInteger(data);
      WallCopulaId = GetString(data);
      data.ReadBytes(4);
    }

    public PassiveMask PassiveMask { get; set; }

    public string WallCopulaId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => (int)PassiveMask,
        () => WallCopulaId,
        () => 1
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
          bw.Write((int)PassiveMask);
          bw.Write(WallCopulaId.Length);
          bw.Write(encoding.GetBytes(WallCopulaId));
          bw.Write(-1);
        }

        return output.ToArray();
      }
    }
  }
}
