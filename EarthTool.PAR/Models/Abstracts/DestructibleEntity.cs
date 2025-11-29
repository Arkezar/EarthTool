using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class DestructibleEntity : InteractableEntity
  {
    public DestructibleEntity()
    {
    }

    public DestructibleEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      HP = ReadInteger(data);
      HpRegeneration = ReadInteger(data);
      Armor = ReadInteger(data);
      CalorificCapacity = ReadInteger(data);
      DisableResist = ReadInteger(data);
      StoreableFlags = (StoreableFlags)ReadInteger(data);
      StandType = (StandType)ReadInteger(data);
    }

    public int HP { get; set; }

    public int HpRegeneration { get; set; }

    public int Armor { get; set; }

    public int CalorificCapacity { get; set; }

    public int DisableResist { get; set; }

    public StoreableFlags StoreableFlags { get; set; }

    public StandType StandType { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => HP,
        () => HpRegeneration,
        () => Armor,
        () => CalorificCapacity,
        () => DisableResist,
        () => StoreableFlags,
        () => StandType
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write(HP);
      bw.Write(HpRegeneration);
      bw.Write(Armor);
      bw.Write(CalorificCapacity);
      bw.Write(DisableResist);
      bw.Write((int)StoreableFlags);
      bw.Write((int)StandType);

      return output.ToArray();
    }
  }
}
