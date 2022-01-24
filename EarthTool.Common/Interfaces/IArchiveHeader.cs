using System;

namespace EarthTool.Common.Interfaces
{
  public interface IArchiveHeader
  {
    Guid Identifier { get; }

    bool IsValid();

    byte[] ToByteArray();
  }
}