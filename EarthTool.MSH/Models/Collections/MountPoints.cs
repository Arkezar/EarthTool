using EarthTool.MSH.Models.Elements;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.MSH.Models.Collections
{
  public class MountPoints : List<Vector>
  {
    const int NUMBER_OF_MOUNTPOINTS = 4;

    public int NumberOfAvailableMountPoints =>
      this.Count(m => m.Value.Length() > 0);

    public MountPoints(Stream stream)
    {
      AddRange(Enumerable.Range(0, NUMBER_OF_MOUNTPOINTS).Select(_ => new Vector(stream)));
    }
  }
}
