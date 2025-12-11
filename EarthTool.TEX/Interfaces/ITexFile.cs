using System.Collections.Generic;

namespace EarthTool.TEX.Interfaces
{
  public interface ITexFile
  {
    TexHeader Header { get; }
    IEnumerable<IEnumerable<TexImage>> Images { get; }
  }
}
