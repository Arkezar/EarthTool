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
      Selected = ReadString(data);
      Move = ReadString(data);
      Attack = ReadString(data);
      Command = ReadString(data);
      Enemy = ReadString(data);
      Help = ReadString(data);
      FreeWay = ReadString(data);
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
      WriteString(bw, Selected, encoding);
      WriteString(bw, Move, encoding);
      WriteString(bw, Attack, encoding);
      WriteString(bw, Command, encoding);
      WriteString(bw, Enemy, encoding);
      WriteString(bw, Help, encoding);
      WriteString(bw, FreeWay, encoding);

      return output.ToArray();
    }
  }
}
