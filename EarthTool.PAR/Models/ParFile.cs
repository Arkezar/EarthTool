using EarthTool.Common.Interfaces;
using System.Collections.Generic;

namespace EarthTool.PAR.Models
{
  public class ParFile
  {
    public IEarthInfo FileHeader { get; set; }

    public IEnumerable<EntityGroup> Groups { get; set; }

    public IEnumerable<Research> Research { get; set; }
  }
}
