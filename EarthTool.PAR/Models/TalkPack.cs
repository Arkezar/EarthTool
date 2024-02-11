using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class TalkPack : Entity
  {
    public TalkPack(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, fieldTypes)
    {
      Selected = GetString(data);
      Move = GetString(data);
      Attack = GetString(data);
      Command = GetString(data);
      Enemy = GetString(data);
      Help = GetString(data);
      FreeWay = GetString(data);
    }

    public string Selected { get; }

    public string Move { get; }

    public string Attack { get; }

    public string Command { get; }

    public string Enemy { get; }

    public string Help { get; }

    public string FreeWay { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
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
