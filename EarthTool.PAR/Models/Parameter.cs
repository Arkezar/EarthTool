using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.PAR.Models
{
  public class Parameter : Entity
  {
    public Parameter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type)
    {
      Values = fieldTypes.Select(s => s ? (object)GetString(data) : GetInteger(data)).ToList();
    }

    public IEnumerable<object> Values { get; }
  }
}
