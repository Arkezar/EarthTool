using EarthTool.PAR.Enums;
using System.Collections.Generic;

namespace EarthTool.PAR.Models
{
  public abstract class Entity
  {
    public Entity(string name, IEnumerable<int> requiredResearch, EntityClassType type)
    {
      Name = name;
      RequiredResearch = requiredResearch;
      ClassId = type;
    }

    public string Name { get; }

    public IEnumerable<int> RequiredResearch { get; }

    public EntityClassType ClassId { get; }
  }
}
