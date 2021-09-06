using System;
using System.Collections.Generic;

namespace EarthTool.Common.Interfaces
{
  public interface IArchive
  {
    DateTime LastModified { get; }
    IEnumerable<IArchiveResource> Resources { get; }
  }
}