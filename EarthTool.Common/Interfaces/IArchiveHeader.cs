using System;

namespace EarthTool.Common.Interfaces
{
  public interface IArchiveHeader
  {
    Guid ArchiveIdentifier { get; }

    bool IsValid();
  }
}