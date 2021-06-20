using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class MountPoints : List<MountPoint>
  {
    const int NUMBER_OF_MOUNTPOINTS = 4;

    public int NumberOfAvailableMountPoints =>
      this.Count(m => m.IsAvailable);

    public MountPoints(Stream stream)
    {
      AddRange(Enumerable.Range(0, NUMBER_OF_MOUNTPOINTS).Select(_ => new MountPoint(stream)));
    }
  }
}
