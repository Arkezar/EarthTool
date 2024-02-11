using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class EntityGroup
  {
    public EntityGroup(BinaryReader reader)
    {
      var entityFactory = new EntityFactory();

      Faction = (Faction)reader.ReadInt32();
      GroupType = (EntityGroupType)reader.ReadInt32();
      var groupSize = reader.ReadInt32();
      Entities = Enumerable.Range(0, groupSize).Select(i => entityFactory.CreateEntity(reader, GroupType)).ToList();
    }

    public Faction Faction { get; }

    public EntityGroupType GroupType { get; }

    public IEnumerable<Entity> Entities { get; }
  }
}
