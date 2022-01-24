using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.Common.Interfaces
{
  public interface IArchiveCentralDirectory
  {
    public DateTime LastModified { get; }
    public IEnumerable<IArchiveFileHeader> FileHeaders { get; }
    void Add(IArchiveFileHeader fileHeader);
    void Remove(IArchiveFileHeader fileHeader);
    byte[] ToByteArray();
  }
}
