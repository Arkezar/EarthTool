using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Special : InteractableEntity
  {
    public Special(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
    }
  }
}
