using EarthTool.MSH.Models.Elements;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EarthTool.MSH.Models.Collections
{
  public class MountPoints : List<Vector>
  {
    const int NUMBER_OF_MOUNTPOINTS = 4;

    [JsonIgnore]
    public int NumberOfAvailableMountPoints =>
      this.Count(m => m.Value.Length() > 0);

    public MountPoints(Stream stream)
    {
      AddRange(Enumerable.Range(0, NUMBER_OF_MOUNTPOINTS).Select(_ => new Vector(stream)));
    }

    public override string ToString()
    {
      return JsonSerializer.Serialize(this, new JsonSerializerOptions
      {
        WriteIndented = true
      });
    }
  }
}
