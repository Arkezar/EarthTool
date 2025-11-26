using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class MovableEntity : InteractableEntity
  {
    public MovableEntity()
    {
    }

    public MovableEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
    }
  }
}
