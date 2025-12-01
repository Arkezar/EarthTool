using EarthTool.PAR.Extensions;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class TalkPack : TypelessEntity
  {
    public TalkPack()
    {
    }

    public TalkPack(string name, IEnumerable<int> requiredResearch, BinaryReader data)
      : base(name, requiredResearch)
    {
      Selected = data.ReadParameterString();
      Move = data.ReadParameterString();
      Attack = data.ReadParameterString();
      Command = data.ReadParameterString();
      Enemy = data.ReadParameterString();
      Help = data.ReadParameterString();
      FreeWay = data.ReadParameterString();
    }

    public string Selected { get; set; }

    public string Move { get; set; }

    public string Attack { get; set; }

    public string Command { get; set; }

    public string Enemy { get; set; }

    public string Help { get; set; }

    public string FreeWay { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => IsStringMember(
        () => Selected,
        () => Move,
        () => Attack,
        () => Command,
        () => Enemy,
        () => Help,
        () => FreeWay
      );
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.WriteParameterString(Selected, encoding);
      bw.WriteParameterString(Move, encoding);
      bw.WriteParameterString(Attack, encoding);
      bw.WriteParameterString(Command, encoding);
      bw.WriteParameterString(Enemy, encoding);
      bw.WriteParameterString(Help, encoding);
      bw.WriteParameterString(FreeWay, encoding);

      return output.ToArray();
    }
  }
}
