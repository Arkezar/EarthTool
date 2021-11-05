using EarthTool.MSH.Models.Elements;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.MSH.Models.Collections
{
  public class SpotLights : List<SpotLight>
  {
    const int NUMBER_OF_LIGHTS = 4;

    public int NumberOfAvailableLights =>
      this.Count(l => l.IsAvailable);

    public SpotLights(Stream stream)
    {
      AddRange(Enumerable.Range(0, NUMBER_OF_LIGHTS).Select(_ => new SpotLight(stream)));
    }
  }
}
