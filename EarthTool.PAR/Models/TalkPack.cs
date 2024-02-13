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
      Selected = GetString(data);
      Move = GetString(data);
      Attack = GetString(data);
      Command = GetString(data);
      Enemy = GetString(data);
      Help = GetString(data);
      FreeWay = GetString(data);
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
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(Selected.Length);
          bw.Write(encoding.GetBytes(Selected));
          bw.Write(Move.Length);
          bw.Write(encoding.GetBytes(Move));
          bw.Write(Attack.Length);
          bw.Write(encoding.GetBytes(Attack));
          bw.Write(Command.Length);
          bw.Write(encoding.GetBytes(Command));
          bw.Write(Enemy.Length);
          bw.Write(encoding.GetBytes(Enemy));
          bw.Write(Help.Length);
          bw.Write(encoding.GetBytes(Help));
          bw.Write(FreeWay.Length);
          bw.Write(encoding.GetBytes(FreeWay));
        }

        return output.ToArray();
      }
    }
  }
}