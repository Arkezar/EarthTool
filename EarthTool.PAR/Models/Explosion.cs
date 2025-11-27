using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Explosion : DestructibleEntity
  {
    public Explosion()
    {
    }

    public Explosion(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      ExplosionTicks = GetInteger(data);
      ExplosionFlags = (ExplosionFlags)GetInteger(data);
    }

    public int ExplosionTicks { get; set; }

    public ExplosionFlags ExplosionFlags { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => ExplosionTicks,
        () => (int)ExplosionFlags
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
          bw.Write(ExplosionTicks);
          bw.Write((int)ExplosionFlags);
        }

        return output.ToArray();
      }
    }
  }
}
