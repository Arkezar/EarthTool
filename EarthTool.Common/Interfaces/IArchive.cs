using System;
using System.Collections.Generic;

namespace EarthTool.Common.Interfaces
{
  public interface IArchive
  {
    string FilePath { get; }

    IArchiveHeader Header { get; }

    IArchiveCentralDirectory CentralDirectory { get; }

    byte[] ExtractResource(IArchiveFileHeader resourceHeader);

    byte[] ToByteArray();
  }
}