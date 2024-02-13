using EarthTool.PAR.Enums;
using System.Collections.Generic;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class TypelessEntity : Entity
  {
    public TypelessEntity()
    {
    }

    public TypelessEntity(string name, IEnumerable<int> requiredResearch)
      : base(name, requiredResearch, EntityClassType.None)
    {
    }

    public override IEnumerable<bool> FieldTypes { get; set; }
  }
}