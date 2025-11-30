using EarthTool.Common.Interfaces;
using EarthTool.PAR.Enums;
using EarthTool.PAR.Factories;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EarthTool.PAR.Models
{
  public class EntityGroup : IBinarySerializable
  {
    public EntityGroup()
    {
    }

    public EntityGroup(BinaryReader reader)
    {
      EntityFactory entityFactory = new EntityFactory();

      Faction = (Faction)reader.ReadInt32();
      GroupType = (EntityGroupType)reader.ReadInt32();
      int groupSize = reader.ReadInt32();
      Entities = Enumerable.Range(0, groupSize).Select(i => entityFactory.CreateEntity(reader, GroupType)).ToList();
    }
    
    public string Name => Entities.Select(e => ExtractCommonName(e.Name)).Distinct().Aggregate((a, b) => $"{a}, {b}");

    public Faction Faction { get; set; }

    public EntityGroupType GroupType { get; set; }

    public IEnumerable<Entity> Entities { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write((int)Faction);
      bw.Write((int)GroupType);
      bw.Write(Entities.Count());
      bw.Write(Entities.SelectMany(e => e.ToByteArray(encoding)).ToArray());

      return output.ToArray();
    }

    private static string ExtractCommonName(string name)
    {
      var regex = new Regex(@"\D+", RegexOptions.Compiled);
      var match = regex.Match(name);
      return match.Success ? match.Value : name;
    }
  }
}
