using System.Collections.Generic;

namespace EarthTool.Common.Interfaces
{
  public interface IArchivizer
  {
    IArchive GetArchiveDescriptor(string filePath);
    IArchiveHeader GetArchiveHeader(string filePath);
    IEnumerable<IArchiveResource> GetResourceWithData(string filePath, IEnumerable<IArchiveResource> resources);
    IArchiveResource GetResourceWithData(string filePath, IArchiveResource resource);
    bool VerifyFile(string filePath);
  }
}