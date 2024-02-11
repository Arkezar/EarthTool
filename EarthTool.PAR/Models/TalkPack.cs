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
    public TalkPack(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type)
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
  }
}
