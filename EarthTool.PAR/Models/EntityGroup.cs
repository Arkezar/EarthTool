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
    public EntityGroup(Stream data)
    {
      var entityFactory = new EntityFactory();

      Faction = (Faction)BitConverter.ToInt32(data.ReadBytes(4));
      GroupType = (EntityGroupType)BitConverter.ToInt32(data.ReadBytes(4));
      var groupSize = BitConverter.ToInt32(data.ReadBytes(4));
      Entities = Enumerable.Range(0, groupSize).Select(i => entityFactory.CreateEntity(data, GroupType)).ToList();
    }

    public Faction Faction { get; }

    public EntityGroupType GroupType { get; }

    public IEnumerable<Entity> Entities { get; }
  }
}
