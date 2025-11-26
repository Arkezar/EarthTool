using System;

namespace EarthTool.Common.Interfaces
{
  public interface IArchiveFactory
  {
    IArchive OpenArchive(string path);
    IArchive NewArchive();
    IArchive NewArchive(DateTime lastModification);
    IArchive NewArchive(DateTime lastModification, Guid guid);
  }
}
