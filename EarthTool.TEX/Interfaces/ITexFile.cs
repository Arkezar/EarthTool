using System.Collections.Generic;

namespace EarthTool.TEX.Interfaces
{
  public interface ITexFile
  {
    bool HasHeader { get; }
    TexHeader Header { get; }
    IEnumerable<IEnumerable<TexImage>> Images { get; }
  }
}