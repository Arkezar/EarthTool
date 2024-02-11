using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;

namespace EarthTool.PAR.Models
{
  public class SpecialUpdateLink : Entity
  {
    public SpecialUpdateLink(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type)
    {
      Value = GetString(data);
      data.ReadBytes(4);
    }

    public string Value { get; }
  }
}
